using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;

namespace Fmc.Application.Services;

public interface ICafeteriaCouponService
{
    Task<CafeteriaCouponsResponse> GetAvailableAsync(Guid cafeteriaId, ConsumerTier? viewerTier, CancellationToken ct = default);
}

public class CafeteriaCouponService(
    ICafeteriaRepository cafeterias,
    IEnterpriseUserRepository enterpriseUsers,
    IEnterpriseCouponRepository coupons) : ICafeteriaCouponService
{
    public async Task<CafeteriaCouponsResponse> GetAvailableAsync(
        Guid cafeteriaId,
        ConsumerTier? viewerTier,
        CancellationToken ct = default)
    {
        var cafe = await cafeterias.GetByIdAsync(cafeteriaId, ct)
            ?? throw new KeyNotFoundException("Cafetería no encontrada.");

        var isPremiumConsumer = viewerTier == ConsumerTier.Premium;
        if (!isPremiumConsumer)
            return new CafeteriaCouponsResponse(false, null, []);

        var (weekStart, weekEnd) = CouponWeek.CurrentBounds();
        CafeteriaCouponDto? platform = null;
        if (cafe.DiscountPercent > 0)
        {
            platform = new CafeteriaCouponDto(
                null,
                "platform",
                CouponKind.Percent,
                cafe.DiscountPercent,
                null,
                $"Beneficio Premium FMC — {cafe.DiscountPercent}%",
                "Válido para consumidores con plan Premium esta semana.",
                BuildPlatformCode(cafeteriaId),
                weekStart,
                weekEnd);
        }

        var eu = await enterpriseUsers.GetByCafeteriaIdAsync(cafeteriaId, ct);
        var business = new List<CafeteriaCouponDto>();
        if (eu?.SubscriptionTier == EnterpriseSubscriptionTier.Premium)
        {
            var list = await coupons.ListByCafeteriaIdAsync(cafeteriaId, ct);
            business = list
                .Where(c => c.IsActive && CouponWeek.Contains(c.ValidFrom, c.ValidUntil))
                .Select(c => new CafeteriaCouponDto(
                    c.Id,
                    "business",
                    c.Kind,
                    c.Kind == CouponKind.Percent ? c.DiscountPercent : null,
                    c.Kind == CouponKind.FixedAmount ? c.FixedAmountArs : null,
                    c.Title,
                    c.Description,
                    c.Code,
                    c.ValidFrom,
                    c.ValidUntil))
                .ToList();
        }

        return new CafeteriaCouponsResponse(true, platform, business);
    }

    private static string BuildPlatformCode(Guid cafeteriaId) =>
        $"FMC-{cafeteriaId.ToString("N")[..6].ToUpperInvariant()}-W{CouponWeek.WeekNumber()}";
}
