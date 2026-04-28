using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fmc.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fmc.Api.Services;

public interface IJwtTokenService
{
    string CreateConsumerToken(Guid userId, string email, ConsumerTier tier);
    string CreateEnterpriseToken(Guid enterpriseUserId, string email, Guid cafeteriaId, EnterpriseSubscriptionTier subscriptionTier);
}

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 60;
}

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _opt = options.Value;

    public string CreateConsumerToken(Guid userId, string email, ConsumerTier tier)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role, AuthRoles.Consumer),
            new("tier", tier.ToString()),
        };

        return CreateToken(claims);
    }

    public string CreateEnterpriseToken(
        Guid enterpriseUserId,
        string email,
        Guid cafeteriaId,
        EnterpriseSubscriptionTier subscriptionTier)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, enterpriseUserId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role, AuthRoles.Enterprise),
            new("cafeteria_id", cafeteriaId.ToString()),
            new("enterprise_subscription_tier", subscriptionTier.ToString()),
        };
        return CreateToken(claims);
    }

    private string CreateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.ExpiryMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class AuthRoles
{
    public const string Consumer = "consumer";
    public const string Enterprise = "enterprise";
}
