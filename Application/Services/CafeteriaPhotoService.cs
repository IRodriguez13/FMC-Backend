using Fmc.Application.Caching;
using Fmc.Application.Configuration;
using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Constants;
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

    Task DeleteAsync(
        Guid cafeteriaId,
        Guid photoId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default);
}

public class CafeteriaPhotoService(
    ICafeteriaRepository cafeterias,
    ICafeteriaPhotoRepository photos,
    IEnterpriseUserRepository enterpriseUsers,
    IFileStorageService storage,
    IDiscoveryReadCache discoveryCache,
    IOptions<MediaOptions> mediaOptions)
    : ICafeteriaPhotoService
{
    private const string PhotoNotFound = "Foto no encontrada.";
    private const string PhotoForbidden = "No podés gestionar las fotos de este local.";
    private const string EnterpriseOnly = "Solo el negocio puede gestionar las fotos del local.";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private readonly MediaOptions _media = mediaOptions.Value;

    public Task<CafeteriaPhotosResponse> ListAsync(Guid cafeteriaId, CancellationToken ct = default) =>
        discoveryCache.GetOrCreateAsync(
            DiscoveryCacheKeys.Photos(cafeteriaId),
            DiscoveryCacheTtl.Photos,
            async innerCt =>
            {
                await EnsureCafeteriaExistsAsync(cafeteriaId, innerCt);
                var list = await photos.ListByCafeteriaIdAsync(cafeteriaId, innerCt);
                return new CafeteriaPhotosResponse(list.Select(Map).ToList());
            },
            ct);

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
        await EnsureEnterpriseOwnsCafeteriaAsync(cafeteriaId, authorUserId, authorRole, ct);
        ValidateImage(contentLength, contentType);

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
        discoveryCache.InvalidateDiscovery();
        return Map(saved);
    }

    public async Task DeleteAsync(
        Guid cafeteriaId,
        Guid photoId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default)
    {
        await EnsureCafeteriaExistsAsync(cafeteriaId, ct);
        await EnsureEnterpriseOwnsCafeteriaAsync(cafeteriaId, authorUserId, authorRole, ct);

        var photo = await photos.GetByIdAsync(photoId, ct)
            ?? throw new KeyNotFoundException(PhotoNotFound);

        if (photo.CafeteriaId != cafeteriaId)
            throw new KeyNotFoundException(PhotoNotFound);

        await photos.DeleteAsync(photo, ct);
        discoveryCache.InvalidateDiscovery();
    }

    private async Task EnsureEnterpriseOwnsCafeteriaAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct)
    {
        if (authorRole != AuthRoles.Enterprise)
            throw new UnauthorizedAccessException(EnterpriseOnly);

        var enterprise = await enterpriseUsers.GetByIdAsync(authorUserId, ct)
            ?? throw new UnauthorizedAccessException(PhotoForbidden);

        if (enterprise.CafeteriaId != cafeteriaId)
            throw new UnauthorizedAccessException(PhotoForbidden);
    }

    private void ValidateImage(long contentLength, string contentType)
    {
        if (contentLength <= 0)
            throw new ArgumentException("Archivo vacío.");

        if (contentLength > _media.MaxFileSizeBytes)
            throw new ArgumentException($"La imagen supera el tamaño máximo de {_media.MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new ArgumentException("Formato no permitido. Usá JPEG, PNG o WebP.");
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
