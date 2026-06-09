using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface ICafeteriaPhotoRepository
{
    Task<IReadOnlyList<CafeteriaPhoto>> ListByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default);

    Task<CafeteriaPhoto> AddAsync(CafeteriaPhoto photo, CancellationToken ct = default);
}
