using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface ICafeteriaReviewRepository
{
    Task<IReadOnlyList<CafeteriaReview>> ListByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default);

    Task<CafeteriaReview?> GetByAuthorAsync(
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default);

    Task<CafeteriaReview> AddAsync(CafeteriaReview review, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
