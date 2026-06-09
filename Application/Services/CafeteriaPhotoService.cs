using Fmc.Application.Configuration;
using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Fmc.Application.Services;

public interface ICafeteriaPhotoService
{
    Task<CafeteriaPhotosResponse> ListAsync(Guid cafeteriaId, CancellationToken ct = default);

    Task<CafeteriaPhotoDto> UploadAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default);
}

public class CafeteriaPhotoService(
    ICafeteriaRepository cafeterias,
    ICafeteriaPhotoRepository photos,
    IFileStorageService storage,
    IOptions<MediaOptions> mediaOptions)
    : ICafeteriaPhotoService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private readonly MediaOptions _media = mediaOptions.Value;

    public async Task<CafeteriaPhotosResponse> ListAsync(Guid cafeteriaId, CancellationToken ct = default)
    {
        await EnsureCafeteriaExistsAsync(cafeteriaId, ct);
        var list = await photos.ListByCafeteriaIdAsync(cafeteriaId, ct);
        return new CafeteriaPhotosResponse(list.Select(Map).ToList());
    }

    public async Task<CafeteriaPhotoDto> UploadAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default)
    {
        await EnsureCafeteriaExistsAsync(cafeteriaId, ct);

        if (contentLength <= 0)
            throw new ArgumentException("Archivo vacío.");

        if (contentLength > _media.MaxFileSizeBytes)
            throw new ArgumentException($"La imagen supera el tamaño máximo de {_media.MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new ArgumentException("Formato no permitido. Usá JPEG, PNG o WebP.");

        var storageKey = await storage.SaveImageAsync(content, contentType, ct);
        var now = DateTimeOffset.UtcNow;

        var entity = new CafeteriaPhoto
        {
            Id = Guid.NewGuid(),
            CafeteriaId = cafeteriaId,
            StorageKey = storageKey,
            ContentType = contentType,
            AuthorUserId = authorUserId,
            AuthorRole = authorRole,
            CreatedAt = now,
        };

        var saved = await photos.AddAsync(entity, ct);
        return Map(saved);
    }

    private async Task EnsureCafeteriaExistsAsync(Guid cafeteriaId, CancellationToken ct)
    {
        _ = await cafeterias.GetByIdAsync(cafeteriaId, ct)
            ?? throw new KeyNotFoundException("Cafetería no encontrada.");
    }

    private CafeteriaPhotoDto Map(CafeteriaPhoto p) =>
        new(
            p.Id,
            p.CafeteriaId,
            storage.GetPublicUrl(p.StorageKey),
            p.ContentType,
            p.AuthorUserId,
            p.AuthorRole,
            p.CreatedAt);
}
