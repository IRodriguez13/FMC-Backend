using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;

namespace Fmc.Application.Services;

public interface IEnterpriseStatsService
{
    Task<EnterpriseCafeteriaStatsDto> GetMineAsync(Guid enterpriseUserId, CancellationToken ct = default);
}

public class EnterpriseStatsService(
    IEnterpriseUserRepository enterpriseUsers,
    ICafeteriaPhotoRepository photos,
    ICafeteriaReviewRepository reviews,
    IConsumerFavoriteRepository favorites,
    IEnterpriseCouponRepository coupons) : IEnterpriseStatsService
{
    public async Task<EnterpriseCafeteriaStatsDto> GetMineAsync(Guid enterpriseUserId, CancellationToken ct = default)
    {
        var eu = await enterpriseUsers.GetByIdAsync(enterpriseUserId, ct)
            ?? throw new KeyNotFoundException("Cuenta enterprise no encontrada.");

        var cafe = eu.Cafeteria;
        var cafeteriaId = cafe.Id;
        var (weekStart, weekEnd) = CouponWeek.CurrentBounds();

        var photoList = await photos.ListByCafeteriaIdAsync(cafeteriaId, ct);
        var reviewSummary = await reviews.GetSummariesByCafeteriaIdsAsync([cafeteriaId], ct);
        var favoriteCount = await favorites.CountByCafeteriaIdAsync(cafeteriaId, ct);
        var activeCoupons = await coupons.CountActiveForWeekAsync(cafeteriaId, weekStart, weekEnd, ct);

        double? avg = null;
        var reviewCount = 0;
        if (reviewSummary.TryGetValue(cafeteriaId, out var summary))
        {
            reviewCount = summary.TotalCount;
            if (summary.TotalCount > 0) avg = summary.AverageRating;
        }

        return new EnterpriseCafeteriaStatsDto(
            avg,
            reviewCount,
            photoList.Count,
            favoriteCount,
            activeCoupons,
            eu.SubscriptionTier,
            cafe.ListingActive,
            cafe.DiscountPercent,
            weekStart,
            weekEnd);
    }
}
