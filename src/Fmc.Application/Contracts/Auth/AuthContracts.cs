using Fmc.Domain.Entities;

namespace Fmc.Application.Contracts.Auth;

public record ConsumerRegisterRequest(string Email, string Password);
public record ConsumerLoginRequest(string Email, string Password);

public record EnterpriseRegisterRequest(
    string Email,
    string Password,
    string? CafeteriaName,
    string? CafeteriaDescription,
    string? CafeteriaAddress,
    double? Latitude,
    double? Longitude);

public record EnterpriseLoginRequest(string Email, string Password);

public record AuthTokenResponse(
    string Token,
    string Role,
    ConsumerTier? Tier,
    Guid? CafeteriaId,
    EnterpriseSubscriptionTier? EnterpriseSubscriptionTier);
