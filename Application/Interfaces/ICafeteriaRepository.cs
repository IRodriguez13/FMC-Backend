using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface ICafeteriaRepository
{
    Task<Cafeteria?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cafeteria?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Cafeterías con cuenta Enterprise y listado activo (reglas de negocio descubrimiento).</summary>
    Task<IReadOnlyList<Cafeteria>> GetListedForDiscoveryAsync(CancellationToken ct = default);

    Task<Cafeteria> AddAsync(Cafeteria cafeteria, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
