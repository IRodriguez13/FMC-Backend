using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface IEnterpriseCouponRepository
{
    Task<IReadOnlyList<EnterpriseCoupon>> ListByCafeteriaIdAsync(Guid cafeteriaId, CancellationToken ct = default);
    Task<int> CountActiveForWeekAsync(Guid cafeteriaId, DateTimeOffset weekStart, DateTimeOffset weekEnd, CancellationToken ct = default);
    Task<EnterpriseCoupon?> GetByIdAsync(Guid couponId, CancellationToken ct = default);
    Task<EnterpriseCoupon?> GetByCodeAsync(Guid cafeteriaId, string code, CancellationToken ct = default);
    Task<EnterpriseCoupon> AddAsync(EnterpriseCoupon coupon, CancellationToken ct = default);
    Task DeleteAsync(EnterpriseCoupon coupon, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
