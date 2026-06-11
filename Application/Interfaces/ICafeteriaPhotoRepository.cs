using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface ICafeteriaPhotoRepository
{
    Task<IReadOnlyList<CafeteriaPhoto>> ListByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default);

    /// <summary>Última foto por cafetería (para portada en listados).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetLatestStorageKeyByCafeteriaIdsAsync(
        IEnumerable<Guid> cafeteriaIds,
        CancellationToken ct = default);

    Task<CafeteriaPhoto> AddAsync(CafeteriaPhoto photo, CancellationToken ct = default);
}
