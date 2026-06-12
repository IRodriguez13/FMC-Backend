using Fmc.Application.Interfaces;
using Fmc.Domain.Abstractions;

namespace Fmc.Application.Services;

/// <summary>Operaciones CRUD comunes para entidades author-owned bajo una cafetería.</summary>
public abstract class AuthorOwnedCrudServiceBase<TEntity>(
    ICafeteriaRepository cafeterias,
    IAuthorOwnedRepository<TEntity> ownedRepository) where TEntity : class, IAuthorOwnedEntity
{
    protected IAuthorOwnedRepository<TEntity> OwnedRepository => ownedRepository;

    protected Task EnsureCafeteriaExistsAsync(Guid cafeteriaId, CancellationToken ct) =>
        CafeteriaGuard.EnsureExistsAsync(cafeterias, cafeteriaId, ct);

    protected async Task<TEntity> RequireOwnedEntityAsync(
        Guid cafeteriaId,
        Guid entityId,
        Guid authorUserId,
        string authorRole,
        string notFoundMessage,
        string forbiddenMessage,
        CancellationToken ct)
    {
        var entity = await ownedRepository.GetByIdAsync(entityId, ct)
            ?? throw new KeyNotFoundException(notFoundMessage);

        if (entity.CafeteriaId != cafeteriaId)
            throw new KeyNotFoundException(notFoundMessage);

        if (entity.AuthorUserId != authorUserId || entity.AuthorRole != authorRole)
            throw new UnauthorizedAccessException(forbiddenMessage);

        return entity;
    }

    protected async Task DeleteOwnedAsync(
        Guid cafeteriaId,
        Guid entityId,
        Guid authorUserId,
        string authorRole,
        string notFoundMessage,
        string forbiddenMessage,
        CancellationToken ct)
    {
        var entity = await RequireOwnedEntityAsync(
            cafeteriaId, entityId, authorUserId, authorRole, notFoundMessage, forbiddenMessage, ct);
        await ownedRepository.DeleteAsync(entity, ct);
    }
}

internal static class CafeteriaGuard
{
    internal static async Task EnsureExistsAsync(
        ICafeteriaRepository cafeterias,
        Guid cafeteriaId,
        CancellationToken ct)
    {
        _ = await cafeterias.GetByIdAsync(cafeteriaId, ct)
            ?? throw new KeyNotFoundException("Cafetería no encontrada.");
    }
}
