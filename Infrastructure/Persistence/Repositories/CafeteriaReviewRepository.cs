using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class CafeteriaReviewRepository(AppDbContext db)
    : AuthorOwnedRepository<CafeteriaReview>(db), ICafeteriaReviewRepository
{
    public async Task<IReadOnlyList<CafeteriaReview>> ListByCafeteriaIdAsync(
        Guid cafeteriaId,
        CancellationToken ct = default)
    {
        var list = await Db.CafeteriaReviews
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

        var summaries = await Db.CafeteriaReviews
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
}
