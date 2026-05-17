using Fmc.Domain.Entities;

namespace Fmc.Application.Services;

/// <summary>Distancia geográfica y ponderación en listados (Enterprise Premium).</summary>
public static class GeoRanking
{
    /// <summary>Distancia en metros entre dos puntos WGS84 (Haversine).</summary>
    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        static double ToRad(double d) => d * (Math.PI / 180);
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    /// <summary>
    /// Distancia efectiva para ordenar: restamos metros según suscripción Enterprise Premium (ponderación).
    /// Enterprise Standard usa boost 0.
    /// </summary>
    public static double EffectiveSortDistanceMeters(double distanceMeters, double rankingBoostMeters) =>
        distanceMeters - rankingBoostMeters;

    /// <summary>Boost solo para cuentas Enterprise con suscripción Premium.</summary>
    public static double RankingBoostMeters(
        EnterpriseSubscriptionTier enterpriseTier,
        double premiumEnterpriseBoostMeters) =>
        enterpriseTier == EnterpriseSubscriptionTier.Premium ? premiumEnterpriseBoostMeters : 0;
}
