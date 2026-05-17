using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class CafeteriaRepository(AppDbContext db) : ICafeteriaRepository
{
    public Task<Cafeteria?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Cafeterias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Cafeteria?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Cafeterias.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Cafeteria>> GetListedForDiscoveryAsync(CancellationToken ct = default)
    {
        var list = await db.Cafeterias
            .AsNoTracking()
            .Include(c => c.EnterpriseUser)
            .Where(c => c.EnterpriseUser != null && c.ListingActive)
            .ToListAsync(ct);

        return list;
    }

    public async Task<Cafeteria> AddAsync(Cafeteria cafeteria, CancellationToken ct = default)
    {
        db.Cafeterias.Add(cafeteria);
        await db.SaveChangesAsync(ct);
        return cafeteria;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
