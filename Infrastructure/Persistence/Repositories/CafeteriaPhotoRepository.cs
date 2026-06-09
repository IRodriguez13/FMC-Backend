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
        return await db.CafeteriaPhotos
            .AsNoTracking()
            .Where(p => p.CafeteriaId == cafeteriaId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<CafeteriaPhoto> AddAsync(CafeteriaPhoto photo, CancellationToken ct = default)
    {
        db.CafeteriaPhotos.Add(photo);
        await db.SaveChangesAsync(ct);
        return photo;
    }
}
