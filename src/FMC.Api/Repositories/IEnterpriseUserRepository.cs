namespace Fmc.Api.Repositories;

public interface IEnterpriseUserRepository
{
    Task<EnterpriseUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseUser?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<EnterpriseUser?> GetByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default);
    Task<EnterpriseUser> AddAsync(EnterpriseUser user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class EnterpriseUserRepository(AppDbContext db) : IEnterpriseUserRepository
{
    public Task<EnterpriseUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.EnterpriseUsers.Include(e => e.Cafeteria).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<EnterpriseUser?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default) =>
        db.EnterpriseUsers.Include(e => e.Cafeteria).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<EnterpriseUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.EnterpriseUsers.Include(e => e.Cafeteria).FirstOrDefaultAsync(x => x.Email == email, ct);

    public Task<EnterpriseUser?> GetByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default) =>
        db.EnterpriseUsers.Include(e => e.Cafeteria).FirstOrDefaultAsync(x => x.CafeteriaId == cafeteriaId, ct);

    public async Task<EnterpriseUser> AddAsync(EnterpriseUser user, CancellationToken ct = default)
    {
        db.EnterpriseUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
