using Fmc.Domain.Entities;

namespace Fmc.Application.Contracts;

public record ConsumerProfileDto(Guid Id, string Email, ConsumerTier Tier);

public record ConsumerTierUpdateRequest(ConsumerTier Tier);

public record ConsumerTierPatchResponse(string Token, ConsumerProfileDto Profile);
