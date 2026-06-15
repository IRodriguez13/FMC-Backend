using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface IConsumerFavoriteRepository
{
    Task<IReadOnlyList<ConsumerFavorite>> ListByConsumerIdAsync(Guid consumerUserId, CancellationToken ct = default);
    Task<ConsumerFavorite?> GetAsync(Guid consumerUserId, Guid cafeteriaId, CancellationToken ct = default);
    Task<int> CountByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default);
    Task<ConsumerFavorite> AddAsync(ConsumerFavorite favorite, CancellationToken ct = default);
    Task DeleteAsync(ConsumerFavorite favorite, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
