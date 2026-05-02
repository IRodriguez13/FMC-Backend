using Fmc.Application.Configuration;
using Fmc.Application.Contracts.Discovery;
using Fmc.Application.Interfaces;
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
    IOptions<DiscoveryOptions> discoveryOptions) : ICafeteriaDiscoveryService
{
    private readonly DiscoveryOptions _disc = discoveryOptions.Value;

    public async Task<NearbyCafeteriasResponse> GetNearbyAsync(NearbyQuery query, CancellationToken ct = default)
    {
        var maxResults = query.ViewerTier == ConsumerTier.Premium
            ? _disc.PremiumTierMaxResults
            : _disc.FreeTierMaxResults;

        var maxRadiusKm = query.ViewerTier == ConsumerTier.Premium
            ? _disc.PremiumTierMaxRadiusKm
            : _disc.FreeTierMaxRadiusKm;

        var radiusKm = query.RadiusKm.HasValue
            ? Math.Min(query.RadiusKm.Value, maxRadiusKm)
            : maxRadiusKm;

        var listed = await cafeteriaRepository.GetListedForDiscoveryAsync(ct);
        var radiusM = radiusKm * 1000;

        var items = listed
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
            .Select(x => new NearbyCafeteriaItem(
                x.c.Id,
                x.c.Name,
                x.c.Description,
                x.c.Address,
                x.c.Latitude,
                x.c.Longitude,
                Math.Round(x.distanceM, 1),
                x.SubscriptionTier,
                x.DiscountPercent))
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
