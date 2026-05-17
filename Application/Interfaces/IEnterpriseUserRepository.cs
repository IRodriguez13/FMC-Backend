using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface IEnterpriseUserRepository
{
    Task<EnterpriseUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseUser?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<EnterpriseUser?> GetByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<EnterpriseUser> AddAsync(EnterpriseUser user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
