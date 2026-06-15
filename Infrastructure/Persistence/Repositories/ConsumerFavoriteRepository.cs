using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class ConsumerFavoriteRepository(AppDbContext db) : IConsumerFavoriteRepository
{
    public async Task<IReadOnlyList<ConsumerFavorite>> ListByConsumerIdAsync(
        Guid consumerUserId,
        CancellationToken ct = default)
    {
        var list = await db.ConsumerFavorites
            .AsNoTracking()
            .Where(f => f.ConsumerUserId == consumerUserId)
            .ToListAsync(ct);

        return list.OrderByDescending(f => f.CreatedAt).ToList();
    }

    public Task<ConsumerFavorite?> GetAsync(Guid consumerUserId, Guid cafeteriaId, CancellationToken ct = default) =>
        db.ConsumerFavorites.FirstOrDefaultAsync(
            f => f.ConsumerUserId == consumerUserId && f.CafeteriaId == cafeteriaId, ct);

    public Task<int> CountByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default) =>
        db.ConsumerFavorites.CountAsync(f => f.CafeteriaId == cafeteriaId, ct);

    public async Task<ConsumerFavorite> AddAsync(ConsumerFavorite favorite, CancellationToken ct = default)
    {
        db.ConsumerFavorites.Add(favorite);
        await db.SaveChangesAsync(ct);
        return favorite;
    }

    public async Task DeleteAsync(ConsumerFavorite favorite, CancellationToken ct = default)
    {
        db.ConsumerFavorites.Remove(favorite);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
