using Fmc.Domain.Entities;

namespace Fmc.Application.Contracts;

public record NearbyCafeteriaItem(
    Guid Id,
    string Name,
    string? Description,
    string? Address,
    double Latitude,
    double Longitude,
    double DistanceMeters,
    EnterpriseSubscriptionTier SubscriptionTier,
    /// <summary>Solo informado para consumidor Premium; los usuarios Free no ven descuentos.</summary>
    int? DiscountPercent);

public record NearbyCafeteriasResponse(
    double QueryLatitude,
    double QueryLongitude,
    double AppliedRadiusKm,
    ConsumerTier ViewerTier,
    int MaxResultsCap,
    IReadOnlyList<NearbyCafeteriaItem> Items);

public record NearbyQuery(double Latitude, double Longitude, double? RadiusKm, ConsumerTier ViewerTier);
