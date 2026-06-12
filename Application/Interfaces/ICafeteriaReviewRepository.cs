using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface ICafeteriaReviewRepository : IAuthorOwnedRepository<CafeteriaReview>
{
    Task<IReadOnlyList<CafeteriaReview>> ListByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, (double? AverageRating, int TotalCount)>> GetSummariesByCafeteriaIdsAsync(
        IEnumerable<Guid> cafeteriaIds,
        CancellationToken ct = default);
}
