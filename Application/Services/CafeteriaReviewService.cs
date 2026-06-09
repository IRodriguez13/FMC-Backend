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
}

public class CafeteriaReviewService(
    ICafeteriaRepository cafeterias,
    ICafeteriaReviewRepository reviews)
    : ICafeteriaReviewService
{
    private const int MaxTextLength = 2000;

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

        if (request.Rating is < 1 or > 5)
            throw new ArgumentException("La valoración debe estar entre 1 y 5.");

        var text = string.IsNullOrWhiteSpace(request.Text) ? null : request.Text.Trim();
        if (text?.Length > MaxTextLength)
            throw new ArgumentException($"El texto no puede superar {MaxTextLength} caracteres.");

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
                Rating = request.Rating,
                Text = text,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var saved = await reviews.AddAsync(entity, ct);
            return Map(saved);
        }

        existing.Rating = request.Rating;
        existing.Text = text;
        existing.UpdatedAt = now;
        await reviews.SaveChangesAsync(ct);
        return Map(existing);
    }

    private async Task EnsureCafeteriaExistsAsync(Guid cafeteriaId, CancellationToken ct)
    {
        _ = await cafeterias.GetByIdAsync(cafeteriaId, ct)
            ?? throw new KeyNotFoundException("Cafetería no encontrada.");
    }

    private static CafeteriaReviewDto Map(CafeteriaReview r) =>
        new(r.Id, r.CafeteriaId, r.Rating, r.Text, r.AuthorUserId, r.AuthorRole, r.CreatedAt, r.UpdatedAt);
}
