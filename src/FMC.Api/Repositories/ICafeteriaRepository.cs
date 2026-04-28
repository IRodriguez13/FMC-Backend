namespace Fmc.Api.Repositories;

public interface ICafeteriaRepository
{
    Task<Cafeteria?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cafeteria?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Cafeterías con cuenta Enterprise y listado activo (reglas de negocio descubrimiento).</summary>
    Task<IReadOnlyList<Cafeteria>> GetListedForDiscoveryAsync(CancellationToken ct = default);

    Task<Cafeteria> AddAsync(Cafeteria cafeteria, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

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
