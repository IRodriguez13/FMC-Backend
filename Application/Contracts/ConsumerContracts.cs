using Fmc.Domain.Entities;

namespace Fmc.Application.Contracts;

public record ConsumerProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    ConsumerTier Tier,
    string? AvatarUrl);

public record ConsumerProfileUpdateRequest(string? DisplayName);

public record ConsumerTierUpdateRequest(ConsumerTier Tier);

public record ConsumerTierPatchResponse(string Token, ConsumerProfileDto Profile);

public record ConsumerFavoriteItemDto(
    Guid CafeteriaId,
    string Name,
    string? Address,
    double? AverageRating,
    int? ReviewCount,
    string? CoverImageUrl,
    EnterpriseSubscriptionTier SubscriptionTier,
    int? DiscountPercent,
    DateTimeOffset FavoritedAt);

public record ConsumerFavoritesResponse(IReadOnlyList<ConsumerFavoriteItemDto> Items);

public record ConsumerFavoriteIdsResponse(IReadOnlyList<Guid> CafeteriaIds);
