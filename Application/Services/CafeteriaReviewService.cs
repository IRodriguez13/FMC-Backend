using Fmc.Application.Caching;
using Fmc.Application.Configuration;
using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Fmc.Application.Services;

public interface ICafeteriaReviewService
{
    Task<CafeteriaReviewsResponse> ListAsync(Guid cafeteriaId, CancellationToken ct = default);

    Task<CafeteriaReviewDto> CreateOrUpdateAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        CafeteriaReviewCreateRequest request,
        CancellationToken ct = default);

    Task<CafeteriaReviewDto> UpdateAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        CafeteriaReviewUpdateRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default);

    Task<CafeteriaReviewDto> UploadPhotoAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default);

    Task DeletePhotoAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default);
}

public class CafeteriaReviewService(
    ICafeteriaRepository cafeterias,
    ICafeteriaReviewRepository reviews,
    IFileStorageService storage,
    IDiscoveryReadCache discoveryCache,
    IOptions<MediaOptions> mediaOptions)
    : AuthorOwnedCrudServiceBase<CafeteriaReview>(cafeterias, reviews), ICafeteriaReviewService
{
    private const int MaxTextLength = 2000;
    private const string ReviewNotFound = "Reseña no encontrada.";
    private const string ReviewForbidden = "No podés modificar esta reseña.";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private readonly MediaOptions _media = mediaOptions.Value;

    public Task<CafeteriaReviewsResponse> ListAsync(Guid cafeteriaId, CancellationToken ct = default) =>
        discoveryCache.GetOrCreateAsync(
            DiscoveryCacheKeys.Reviews(cafeteriaId),
            DiscoveryCacheTtl.Reviews,
            async innerCt =>
            {
                await EnsureCafeteriaExistsAsync(cafeteriaId, innerCt);
                var list = await reviews.ListByCafeteriaIdAsync(cafeteriaId, innerCt);
                var dtos = list.Select(Map).ToList();
                double? avg = dtos.Count > 0 ? dtos.Average(r => r.Rating) : null;
                return new CafeteriaReviewsResponse(dtos, avg, dtos.Count);
            },
            ct);

    public async Task<CafeteriaReviewDto> CreateOrUpdateAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        CafeteriaReviewCreateRequest request,
        CancellationToken ct = default)
    {
        await EnsureCafeteriaExistsAsync(cafeteriaId, ct);
        var (rating, text) = ValidatePayload(request.Rating, request.Text);

        var now = DateTimeOffset.UtcNow;
        var existing = await reviews.GetByAuthorAsync(cafeteriaId, authorUserId, authorRole, ct);

        if (existing is null)
        {
            var entity = new CafeteriaReview
            {
                Id = Guid.NewGuid(),
                CafeteriaId = cafeteriaId,
                AuthorUserId = authorUserId,
                AuthorRole = authorRole,
                Rating = rating,
                Text = text,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var saved = await reviews.AddAsync(entity, ct);
            InvalidateAfterWrite(cafeteriaId);
            return Map(saved);
        }

        ApplyChanges(existing, rating, text, now);
        await reviews.SaveChangesAsync(ct);
        InvalidateAfterWrite(cafeteriaId);
        return Map(existing);
    }

    public async Task<CafeteriaReviewDto> UpdateAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        CafeteriaReviewUpdateRequest request,
        CancellationToken ct = default)
    {
        await EnsureCafeteriaExistsAsync(cafeteriaId, ct);
        var (rating, text) = ValidatePayload(request.Rating, request.Text);

        var entity = await RequireOwnedEntityAsync(
            cafeteriaId, reviewId, authorUserId, authorRole, ReviewNotFound, ReviewForbidden, ct);

        ApplyChanges(entity, rating, text, DateTimeOffset.UtcNow);
        await reviews.SaveChangesAsync(ct);
        InvalidateAfterWrite(cafeteriaId);
        return Map(entity);
    }

    public async Task DeleteAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default)
    {
        await DeleteOwnedAsync(
            cafeteriaId, reviewId, authorUserId, authorRole, ReviewNotFound, ReviewForbidden, ct);
        InvalidateAfterWrite(cafeteriaId);
    }

    public async Task<CafeteriaReviewDto> UploadPhotoAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default)
    {
        await EnsureCafeteriaExistsAsync(cafeteriaId, ct);
        ValidateImage(contentLength, contentType);

        var entity = await RequireOwnedEntityAsync(
            cafeteriaId, reviewId, authorUserId, authorRole, ReviewNotFound, ReviewForbidden, ct);

        var storageKey = await storage.SaveImageAsync(content, contentType, ct);
        entity.PhotoStorageKey = storageKey;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await reviews.SaveChangesAsync(ct);
        InvalidateAfterWrite(cafeteriaId);
        return Map(entity);
    }

    public async Task DeletePhotoAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default)
    {
        await EnsureCafeteriaExistsAsync(cafeteriaId, ct);

        var entity = await RequireOwnedEntityAsync(
            cafeteriaId, reviewId, authorUserId, authorRole, ReviewNotFound, ReviewForbidden, ct);

        if (string.IsNullOrWhiteSpace(entity.PhotoStorageKey))
            throw new KeyNotFoundException("La reseña no tiene foto.");

        entity.PhotoStorageKey = null;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await reviews.SaveChangesAsync(ct);
        InvalidateAfterWrite(cafeteriaId);
    }

    private void InvalidateAfterWrite(Guid cafeteriaId) =>
        discoveryCache.InvalidateDiscovery();

    private void ValidateImage(long contentLength, string contentType)
    {
        if (contentLength <= 0)
            throw new ArgumentException("Archivo vacío.");

        if (contentLength > _media.MaxFileSizeBytes)
            throw new ArgumentException($"La imagen supera el tamaño máximo de {_media.MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new ArgumentException("Formato no permitido. Usá JPEG, PNG o WebP.");
    }

    private static (int Rating, string? Text) ValidatePayload(int rating, string? rawText)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentException("La valoración debe estar entre 1 y 5.");

        var text = string.IsNullOrWhiteSpace(rawText) ? null : rawText.Trim();
        if (text?.Length > MaxTextLength)
            throw new ArgumentException($"El texto no puede superar {MaxTextLength} caracteres.");

        return (rating, text);
    }

    private static void ApplyChanges(CafeteriaReview entity, int rating, string? text, DateTimeOffset updatedAt)
    {
        entity.Rating = rating;
        entity.Text = text;
        entity.UpdatedAt = updatedAt;
    }

    private CafeteriaReviewDto Map(CafeteriaReview r)
    {
        string? photoUrl = string.IsNullOrWhiteSpace(r.PhotoStorageKey)
            ? null
            : storage.GetPublicUrl(r.PhotoStorageKey);

        return new(r.Id, r.CafeteriaId, r.Rating, r.Text, photoUrl, r.AuthorUserId, r.AuthorRole, r.CreatedAt, r.UpdatedAt);
    }
}
