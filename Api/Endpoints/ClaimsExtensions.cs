using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fmc.Domain.Constants;

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

    public static string RequireAuthorRole(this ClaimsPrincipal user)
    {
        if (user.IsInRole(AuthRoles.Consumer))
            return AuthRoles.Consumer;
        if (user.IsInRole(AuthRoles.Enterprise))
            return AuthRoles.Enterprise;
        throw new UnauthorizedAccessException("Rol no autorizado.");
    }
}
