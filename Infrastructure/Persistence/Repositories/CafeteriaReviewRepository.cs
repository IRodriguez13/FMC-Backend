using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class CafeteriaReviewRepository(AppDbContext db) : ICafeteriaReviewRepository
{
    public async Task<IReadOnlyList<CafeteriaReview>> ListByCafeteriaIdAsync(
        Guid cafeteriaId,
        CancellationToken ct = default)
    {
        var list = await db.CafeteriaReviews
            .AsNoTracking()
            .Where(r => r.CafeteriaId == cafeteriaId)
            .ToListAsync(ct);

        return list.OrderByDescending(r => r.UpdatedAt).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, (double? AverageRating, int TotalCount)>> GetSummariesByCafeteriaIdsAsync(
        IEnumerable<Guid> cafeteriaIds,
        CancellationToken ct = default)
    {
        var ids = cafeteriaIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, (double?, int)>();

        var summaries = await db.CafeteriaReviews
            .AsNoTracking()
            .Where(r => ids.Contains(r.CafeteriaId))
            .GroupBy(r => r.CafeteriaId)
            .Select(g => new
            {
                CafeteriaId = g.Key,
                Average = g.Average(r => (double)r.Rating),
                Count = g.Count(),
            })
            .ToListAsync(ct);

        return summaries.ToDictionary(
            s => s.CafeteriaId,
            s => ((double?)Math.Round(s.Average, 1), s.Count));
    }

    public Task<CafeteriaReview?> GetByAuthorAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default) =>
        db.CafeteriaReviews.FirstOrDefaultAsync(
            r => r.CafeteriaId == cafeteriaId
                 && r.AuthorUserId == authorUserId
                 && r.AuthorRole == authorRole,
            ct);

    public async Task<CafeteriaReview> AddAsync(CafeteriaReview review, CancellationToken ct = default)
    {
        db.CafeteriaReviews.Add(review);
        await db.SaveChangesAsync(ct);
        return review;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
