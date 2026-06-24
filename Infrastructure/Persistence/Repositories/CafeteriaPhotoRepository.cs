using Fmc.Application.Interfaces;
using Fmc.Domain.Constants;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class CafeteriaPhotoRepository(AppDbContext db) : ICafeteriaPhotoRepository
{
    public async Task<IReadOnlyList<CafeteriaPhoto>> ListByCafeteriaIdAsync(
        Guid cafeteriaId,
        CancellationToken ct = default)
    {
        return await db.CafeteriaPhotos
            .AsNoTracking()
            .Where(p => p.CafeteriaId == cafeteriaId && p.AuthorRole == AuthRoles.Enterprise)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetLatestStorageKeyByCafeteriaIdsAsync(
        IEnumerable<Guid> cafeteriaIds,
        CancellationToken ct = default)
    {
        var ids = cafeteriaIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await db.CafeteriaPhotos
            .AsNoTracking()
            .Where(p => ids.Contains(p.CafeteriaId) && p.AuthorRole == AuthRoles.Enterprise)
            .GroupBy(p => p.CafeteriaId)
            .Select(g => new
            {
                CafeteriaId = g.Key,
                StorageKey = g.OrderByDescending(p => p.CreatedAt).Select(p => p.StorageKey).First(),
            })
            .ToDictionaryAsync(x => x.CafeteriaId, x => x.StorageKey, ct);
    }

    public Task<CafeteriaPhoto?> GetByIdAsync(Guid photoId, CancellationToken ct = default) =>
        db.CafeteriaPhotos.FirstOrDefaultAsync(p => p.Id == photoId, ct);

    public async Task<CafeteriaPhoto> AddAsync(CafeteriaPhoto photo, CancellationToken ct = default)
    {
        db.CafeteriaPhotos.Add(photo);
        await db.SaveChangesAsync(ct);
        return photo;
    }

    public async Task DeleteAsync(CafeteriaPhoto photo, CancellationToken ct = default)
    {
        db.CafeteriaPhotos.Remove(photo);
        await db.SaveChangesAsync(ct);
    }
}
