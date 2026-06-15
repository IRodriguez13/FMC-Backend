using Fmc.Domain.Entities;

namespace Fmc.Application.Contracts;

public record EnterpriseCafeteriaStatsDto(
    double? AverageRating,
    int ReviewCount,
    int PhotoCount,
    int FavoriteCount,
    int ActiveCouponsThisWeek,
    EnterpriseSubscriptionTier SubscriptionTier,
    bool ListingActive,
    int DiscountPercent,
    DateTimeOffset WeekValidFrom,
    DateTimeOffset WeekValidUntil);

public record EnterpriseCouponDto(
    Guid Id,
    CouponKind Kind,
    int DiscountPercent,
    int FixedAmountArs,
    string Title,
    string? Description,
    string Code,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    bool IsActive);

public record EnterpriseCouponCreateRequest(
    CouponKind Kind,
    int? DiscountPercent,
    int? FixedAmountArs,
    string? Title,
    string? Description,
    string? Code);

public record CafeteriaCouponDto(
    Guid? Id,
    string Source,
    CouponKind Kind,
    int? DiscountPercent,
    int? FixedAmountArs,
    string Title,
    string? Description,
    string Code,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil);

public record CafeteriaCouponsResponse(
    bool ViewerIsPremiumConsumer,
    CafeteriaCouponDto? PlatformCoupon,
    IReadOnlyList<CafeteriaCouponDto> BusinessCoupons);
