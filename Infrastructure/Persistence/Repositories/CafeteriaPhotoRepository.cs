using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class CafeteriaPhotoRepository(AppDbContext db) : ICafeteriaPhotoRepository
{
    public async Task<IReadOnlyList<CafeteriaPhoto>> ListByCafeteriaIdAsync(
        Guid cafeteriaId,
        CancellationToken ct = default)
    {
        var list = await db.CafeteriaPhotos
            .AsNoTracking()
            .Where(p => p.CafeteriaId == cafeteriaId)
            .ToListAsync(ct);

        return list.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetLatestStorageKeyByCafeteriaIdsAsync(
        IEnumerable<Guid> cafeteriaIds,
        CancellationToken ct = default)
    {
        var ids = cafeteriaIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var list = await db.CafeteriaPhotos
            .AsNoTracking()
            .Where(p => ids.Contains(p.CafeteriaId))
            .ToListAsync(ct);

        return list
            .GroupBy(p => p.CafeteriaId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(p => p.CreatedAt).First().StorageKey);
    }

    public async Task<CafeteriaPhoto> AddAsync(CafeteriaPhoto photo, CancellationToken ct = default)
    {
        db.CafeteriaPhotos.Add(photo);
        await db.SaveChangesAsync(ct);
        return photo;
    }
}
