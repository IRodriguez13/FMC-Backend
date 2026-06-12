using Fmc.Domain.Abstractions;

namespace Fmc.Application.Interfaces;

/// <summary>CRUD mínimo para entidades con dueño (AuthorUserId + AuthorRole) en una cafetería.</summary>
public interface IAuthorOwnedRepository<TEntity> where TEntity : class, IAuthorOwnedEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<TEntity?> GetByAuthorAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);

    Task DeleteAsync(TEntity entity, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
