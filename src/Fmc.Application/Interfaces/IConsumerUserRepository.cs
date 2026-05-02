using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface IConsumerUserRepository
{
    Task<ConsumerUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ConsumerUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ConsumerUser> AddAsync(ConsumerUser user, CancellationToken ct = default);
    Task<ConsumerUser?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
