using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

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

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        db.EnterpriseUsers.AnyAsync(x => x.Email == email, ct);

    public async Task<EnterpriseUser> AddAsync(EnterpriseUser user, CancellationToken ct = default)
    {
        db.EnterpriseUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
