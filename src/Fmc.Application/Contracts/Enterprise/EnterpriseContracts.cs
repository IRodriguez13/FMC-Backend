using Fmc.Domain.Entities;

namespace Fmc.Application.Contracts.Enterprise;

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
