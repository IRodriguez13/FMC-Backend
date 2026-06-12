using Fmc.Application.Interfaces;
using Fmc.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class AuthorOwnedRepository<TEntity>(AppDbContext db) : IAuthorOwnedRepository<TEntity>
    where TEntity : class, IAuthorOwnedEntity
{
    protected AppDbContext Db => db;

    protected DbSet<TEntity> Set => db.Set<TEntity>();

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<TEntity?> GetByAuthorAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(
            e => e.CafeteriaId == cafeteriaId
                 && e.AuthorUserId == authorUserId
                 && e.AuthorRole == authorRole,
            ct);

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        Set.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        Set.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
