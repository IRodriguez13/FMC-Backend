using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class ConsumerUserRepository(AppDbContext db) : IConsumerUserRepository
{
    public Task<ConsumerUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ConsumerUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ConsumerUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.ConsumerUsers.FirstOrDefaultAsync(x => x.Email == email, ct);

    public Task<ConsumerUser?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ConsumerUsers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<ConsumerUser> AddAsync(ConsumerUser user, CancellationToken ct = default)
    {
        db.ConsumerUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
