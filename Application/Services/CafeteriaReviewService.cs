using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;

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
}

public class CafeteriaReviewService(
    ICafeteriaRepository cafeterias,
    ICafeteriaReviewRepository reviews)
    : AuthorOwnedCrudServiceBase<CafeteriaReview>(cafeterias, reviews), ICafeteriaReviewService
{
    private const int MaxTextLength = 2000;
    private const string ReviewNotFound = "Reseña no encontrada.";
    private const string ReviewForbidden = "No podés modificar esta reseña.";

    public async Task<CafeteriaReviewsResponse> ListAsync(Guid cafeteriaId, CancellationToken ct = default)
    {
        await EnsureCafeteriaExistsAsync(cafeteriaId, ct);
        var list = await reviews.ListByCafeteriaIdAsync(cafeteriaId, ct);
        var dtos = list.Select(Map).ToList();
        double? avg = dtos.Count > 0 ? dtos.Average(r => r.Rating) : null;
        return new CafeteriaReviewsResponse(dtos, avg, dtos.Count);
    }

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
            return Map(saved);
        }

        ApplyChanges(existing, rating, text, now);
        await reviews.SaveChangesAsync(ct);
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
        return Map(entity);
    }

    public Task DeleteAsync(
        Guid cafeteriaId,
        Guid reviewId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default) =>
        DeleteOwnedAsync(
            cafeteriaId, reviewId, authorUserId, authorRole, ReviewNotFound, ReviewForbidden, ct);

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

    private static CafeteriaReviewDto Map(CafeteriaReview r) =>
        new(r.Id, r.CafeteriaId, r.Rating, r.Text, r.AuthorUserId, r.AuthorRole, r.CreatedAt, r.UpdatedAt);
}
