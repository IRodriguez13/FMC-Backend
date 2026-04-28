using Fmc.Api.Entities;

namespace Fmc.Api.Contracts;

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

public record ConsumerRegisterRequest(string Email, string Password);
public record ConsumerLoginRequest(string Email, string Password);

public record AuthTokenResponse(
    string Token,
    string Role,
    ConsumerTier? Tier,
    Guid? CafeteriaId,
    EnterpriseSubscriptionTier? EnterpriseSubscriptionTier);

public record EnterpriseRegisterRequest(
    string Email,
    string Password,
    string? CafeteriaName,
    string? CafeteriaDescription,
    string? CafeteriaAddress,
    double? Latitude,
    double? Longitude);

public record EnterpriseLoginRequest(string Email, string Password);

public record EnterpriseCafeteriaDto(
    Guid Id,
    string Name,
    string? Description,
    string? Address,
    double Latitude,
    double Longitude,
    EnterpriseSubscriptionTier SubscriptionTier,
    bool ListingActive,
    int DiscountPercent);

public record EnterpriseCafeteriaUpdateRequest(
    string Name,
    string? Description,
    string? Address,
    double Latitude,
    double Longitude,
    int DiscountPercent);

public record EnterpriseSubscriptionTierUpdateRequest(EnterpriseSubscriptionTier SubscriptionTier);

public record ConsumerTierUpdateRequest(ConsumerTier Tier);

public record ConsumerProfileDto(Guid Id, string Email, ConsumerTier Tier);

public record ConsumerTierPatchResponse(string Token, ConsumerProfileDto Profile);
