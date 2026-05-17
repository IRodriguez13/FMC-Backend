using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Fmc.Api.Endpoints;

internal static class ClaimsExtensions
{
    public static Guid RequireUserId(this ClaimsPrincipal user)
    {
        var s = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (Guid.TryParse(s, out var id))
            return id;
        throw new UnauthorizedAccessException("Token inválido.");
    }
}
