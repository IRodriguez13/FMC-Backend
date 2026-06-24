using System.Globalization;
using Fmc.Application.Contracts;
using Fmc.Domain.Entities;

namespace Fmc.Application.Caching;

public static class DiscoveryCacheKeys
{
    public const string ListedSuffix = "listed";

    public static string Nearby(NearbyQuery query, double radiusKm, int maxResults) =>
        $"nearby:{RoundCoord(query.Latitude)}:{RoundCoord(query.Longitude)}:{radiusKm:F2}:{query.ViewerTier}:{query.ExcludeCafeteriaId}:{maxResults}";

    public static string Reviews(Guid cafeteriaId) => $"reviews:{cafeteriaId:N}";

    public static string Photos(Guid cafeteriaId) => $"photos:{cafeteriaId:N}";

    private static string RoundCoord(double value) =>
        value.ToString("F4", CultureInfo.InvariantCulture);
}

public static class DiscoveryCacheTtl
{
    public static readonly TimeSpan Listed = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan Nearby = TimeSpan.FromSeconds(90);
    public static readonly TimeSpan Reviews = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan Photos = TimeSpan.FromMinutes(5);
}
