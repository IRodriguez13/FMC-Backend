using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence.Repositories;

public class EnterpriseCouponRepository(AppDbContext db) : IEnterpriseCouponRepository
{
    public async Task<IReadOnlyList<EnterpriseCoupon>> ListByCafeteriaIdAsync(
        Guid cafeteriaId,
        CancellationToken ct = default)
    {
        var list = await db.EnterpriseCoupons
            .AsNoTracking()
            .Where(c => c.CafeteriaId == cafeteriaId)
            .ToListAsync(ct);

        return list.OrderByDescending(c => c.CreatedAt).ToList();
    }

    public async Task<int> CountActiveForWeekAsync(
        Guid cafeteriaId,
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd,
        CancellationToken ct = default)
    {
        var list = await db.EnterpriseCoupons
            .AsNoTracking()
            .Where(c => c.CafeteriaId == cafeteriaId && c.IsActive)
            .ToListAsync(ct);

        return list.Count(c => c.ValidFrom <= weekEnd && c.ValidUntil >= weekStart);
    }

    public Task<EnterpriseCoupon?> GetByIdAsync(Guid couponId, CancellationToken ct = default) =>
        db.EnterpriseCoupons.FirstOrDefaultAsync(c => c.Id == couponId, ct);

    public Task<EnterpriseCoupon?> GetByCodeAsync(Guid cafeteriaId, string code, CancellationToken ct = default) =>
        db.EnterpriseCoupons.FirstOrDefaultAsync(
            c => c.CafeteriaId == cafeteriaId && c.Code == code, ct);

    public async Task<EnterpriseCoupon> AddAsync(EnterpriseCoupon coupon, CancellationToken ct = default)
    {
        db.EnterpriseCoupons.Add(coupon);
        await db.SaveChangesAsync(ct);
        return coupon;
    }

    public async Task DeleteAsync(EnterpriseCoupon coupon, CancellationToken ct = default)
    {
        db.EnterpriseCoupons.Remove(coupon);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
