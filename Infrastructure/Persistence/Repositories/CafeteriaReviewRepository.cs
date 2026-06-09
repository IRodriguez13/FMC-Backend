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
        return await db.CafeteriaReviews
            .AsNoTracking()
            .Where(r => r.CafeteriaId == cafeteriaId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);
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
