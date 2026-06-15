using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;

namespace Fmc.Application.Services;

public interface IEnterpriseCouponService
{
    Task<IReadOnlyList<EnterpriseCouponDto>> ListMineAsync(Guid enterpriseUserId, CancellationToken ct = default);
    Task<EnterpriseCouponDto> CreateAsync(Guid enterpriseUserId, EnterpriseCouponCreateRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid enterpriseUserId, Guid couponId, CancellationToken ct = default);
}

public class EnterpriseCouponService(
    IEnterpriseUserRepository enterpriseUsers,
    IEnterpriseCouponRepository coupons) : IEnterpriseCouponService
{
    private const int MaxCouponsPerWeek = 3;

    public async Task<IReadOnlyList<EnterpriseCouponDto>> ListMineAsync(
        Guid enterpriseUserId,
        CancellationToken ct = default)
    {
        var eu = await enterpriseUsers.GetByIdAsync(enterpriseUserId, ct)
            ?? throw new KeyNotFoundException("Cuenta enterprise no encontrada.");

        var list = await coupons.ListByCafeteriaIdAsync(eu.CafeteriaId, ct);
        return list.Select(Map).ToList();
    }

    public async Task<EnterpriseCouponDto> CreateAsync(
        Guid enterpriseUserId,
        EnterpriseCouponCreateRequest request,
        CancellationToken ct = default)
    {
        var eu = await enterpriseUsers.GetByIdAsync(enterpriseUserId, ct)
            ?? throw new KeyNotFoundException("Cuenta enterprise no encontrada.");

        if (eu.SubscriptionTier != EnterpriseSubscriptionTier.Premium)
            throw new UnauthorizedAccessException("Los cupones del negocio requieren plan Enterprise Premium.");

        var (weekStart, weekEnd) = CouponWeek.CurrentBounds();
        var activeCount = await coupons.CountActiveForWeekAsync(eu.CafeteriaId, weekStart, weekEnd, ct);
        if (activeCount >= MaxCouponsPerWeek)
            throw new ArgumentException($"Podés publicar hasta {MaxCouponsPerWeek} cupones por semana.");

        var (title, discountPercent, fixedAmountArs) = NormalizeKind(request);
        var code = BuildCode(request.Code, eu.Cafeteria.Name, CouponWeek.WeekNumber());

        if (await coupons.GetByCodeAsync(eu.CafeteriaId, code, ct) is not null)
            throw new ArgumentException("Ya existe un cupón con ese código esta semana.");

        var entity = new EnterpriseCoupon
        {
            Id = Guid.NewGuid(),
            CafeteriaId = eu.CafeteriaId,
            Kind = request.Kind,
            DiscountPercent = discountPercent,
            FixedAmountArs = fixedAmountArs,
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Code = code,
            ValidFrom = weekStart,
            ValidUntil = weekEnd,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await coupons.AddAsync(entity, ct);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid enterpriseUserId, Guid couponId, CancellationToken ct = default)
    {
        var eu = await enterpriseUsers.GetByIdAsync(enterpriseUserId, ct)
            ?? throw new KeyNotFoundException("Cuenta enterprise no encontrada.");

        var coupon = await coupons.GetByIdAsync(couponId, ct)
            ?? throw new KeyNotFoundException("Cupón no encontrado.");

        if (coupon.CafeteriaId != eu.CafeteriaId)
            throw new UnauthorizedAccessException("No podés gestionar este cupón.");

        await coupons.DeleteAsync(coupon, ct);
    }

    private static (string Title, int DiscountPercent, int FixedAmountArs) NormalizeKind(EnterpriseCouponCreateRequest request)
    {
        return request.Kind switch
        {
            CouponKind.Percent => (
                string.IsNullOrWhiteSpace(request.Title) ? $"{request.DiscountPercent ?? 0}% de descuento" : request.Title.Trim(),
                Math.Clamp(request.DiscountPercent ?? 0, 1, 100),
                0),
            CouponKind.FixedAmount => (
                string.IsNullOrWhiteSpace(request.Title) ? $"${request.FixedAmountArs ?? 0} off" : request.Title.Trim(),
                0,
                Math.Clamp(request.FixedAmountArs ?? 0, 100, 500_000)),
            CouponKind.TwoForOne => (
                string.IsNullOrWhiteSpace(request.Title) ? "2x1 en bebida seleccionada" : request.Title.Trim(),
                0,
                0),
            _ => throw new ArgumentException("Tipo de cupón inválido."),
        };
    }

    private static string BuildCode(string? requested, string cafeteriaName, int weekNum)
    {
        if (!string.IsNullOrWhiteSpace(requested))
            return requested.Trim().ToUpperInvariant();

        var slug = new string(cafeteriaName.Where(char.IsLetterOrDigit).Take(8).ToArray()).ToUpperInvariant();
        if (string.IsNullOrEmpty(slug)) slug = "LOCAL";
        return $"{slug}-W{weekNum}";
    }

    private static EnterpriseCouponDto Map(EnterpriseCoupon c) =>
        new(c.Id, c.Kind, c.DiscountPercent, c.FixedAmountArs, c.Title, c.Description, c.Code,
            c.ValidFrom, c.ValidUntil, c.IsActive);
}
