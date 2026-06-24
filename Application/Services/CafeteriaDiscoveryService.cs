using Fmc.Application.Caching;
using Fmc.Application.Configuration;
using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Constants;
using Fmc.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Fmc.Application.Services;

/// <summary>
/// Listados solo de cafeterías registradas por Enterprise con listado activo.
/// Ponderación: Enterprise Premium &gt; Enterprise Standard (misma visibilidad para consumidor Free y Premium;
/// los descuentos solo los ve el consumidor Premium).
/// </summary>
public interface ICafeteriaDiscoveryService
{
    Task<NearbyCafeteriasResponse> GetNearbyAsync(NearbyQuery query, CancellationToken ct = default);
}

public class CafeteriaDiscoveryService(
    ICafeteriaRepository cafeteriaRepository,
    ICafeteriaPhotoRepository photoRepository,
    ICafeteriaReviewRepository reviewRepository,
    IFileStorageService storage,
    IDiscoveryReadCache discoveryCache,
    IOptions<DiscoveryOptions> discoveryOptions) : ICafeteriaDiscoveryService
{
    private readonly DiscoveryOptions _disc = discoveryOptions.Value;

    public Task<NearbyCafeteriasResponse> GetNearbyAsync(NearbyQuery query, CancellationToken ct = default)
    {
        LocationValidation.EnsureWithinCabaServiceArea(query.Latitude, query.Longitude);

        var maxResults = query.ViewerTier == ConsumerTier.Premium
            ? _disc.PremiumTierMaxResults
            : _disc.FreeTierMaxResults;

        var maxRadiusKm = query.ViewerTier == ConsumerTier.Premium
            ? _disc.PremiumTierMaxRadiusKm
            : _disc.FreeTierMaxRadiusKm;

        var radiusKm = query.RadiusKm.HasValue
            ? Math.Min(query.RadiusKm.Value, maxRadiusKm)
            : maxRadiusKm;

        var cacheKey = DiscoveryCacheKeys.Nearby(query, radiusKm, maxResults);
        return discoveryCache.GetOrCreateAsync(
            cacheKey,
            DiscoveryCacheTtl.Nearby,
            ct => BuildNearbyAsync(query, radiusKm, maxResults, ct),
            ct);
    }

    private async Task<NearbyCafeteriasResponse> BuildNearbyAsync(
        NearbyQuery query,
        double radiusKm,
        int maxResults,
        CancellationToken ct)
    {
        var listed = await discoveryCache.GetOrCreateAsync(
            DiscoveryCacheKeys.ListedSuffix,
            DiscoveryCacheTtl.Listed,
            cafeteriaRepository.GetListedForDiscoveryAsync,
            ct);

        var radiusM = radiusKm * 1000;

        var ranked = listed
            .Where(c => LocationValidation.IsWithinCabaServiceArea(c.Latitude, c.Longitude))
            .Where(c => query.ExcludeCafeteriaId == null || c.Id != query.ExcludeCafeteriaId.Value)
            .Select(c =>
            {
                var eu = c.EnterpriseUser!;
                var distanceM = GeoRanking.DistanceMeters(query.Latitude, query.Longitude, c.Latitude, c.Longitude);
                var boost = GeoRanking.RankingBoostMeters(eu.SubscriptionTier, _disc.PremiumEnterpriseRankingBoostMeters);
                var effective = GeoRanking.EffectiveSortDistanceMeters(distanceM, boost);
                var discountVisible = query.ViewerTier == ConsumerTier.Premium
                    ? Math.Clamp(c.DiscountPercent, 0, 100)
                    : (int?)null;

                return new
                {
                    c,
                    eu.SubscriptionTier,
                    distanceM,
                    effective,
                    DiscountPercent = discountVisible,
                };
            })
            .Where(x => x.distanceM <= radiusM)
            .OrderBy(x => x.effective)
            .ThenBy(x => x.c.Name)
            .Take(maxResults)
            .ToList();

        var cafeteriaIds = ranked.Select(x => x.c.Id).ToList();
        var coverKeys = await photoRepository.GetLatestStorageKeyByCafeteriaIdsAsync(cafeteriaIds, ct);
        var reviewSummaries = await reviewRepository.GetSummariesByCafeteriaIdsAsync(cafeteriaIds, ct);

        var items = ranked
            .Select(x =>
            {
                string? coverUrl = coverKeys.TryGetValue(x.c.Id, out var key)
                    ? storage.GetPublicUrl(key)
                    : null;

                double? averageRating = null;
                int? reviewCount = null;
                if (reviewSummaries.TryGetValue(x.c.Id, out var summary) && summary.TotalCount > 0)
                {
                    averageRating = summary.AverageRating;
                    reviewCount = summary.TotalCount;
                }

                return new NearbyCafeteriaItem(
                    x.c.Id,
                    x.c.Name,
                    x.c.Description,
                    x.c.Address,
                    x.c.Latitude,
                    x.c.Longitude,
                    Math.Round(x.distanceM, 1),
                    x.SubscriptionTier,
                    x.DiscountPercent,
                    coverUrl,
                    averageRating,
                    reviewCount);
            })
            .ToList();

        return new NearbyCafeteriasResponse(
            query.Latitude,
            query.Longitude,
            radiusKm,
            query.ViewerTier,
            maxResults,
            items);
    }
}
