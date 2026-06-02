using Fmc.Domain.Constants;
using Fmc.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Fmc.Application.Services;

public static class DiscoveryTierResolver
{
    /// <summary>Sin JWT de consumidor se aplica tier Free (radio y resultados acotados).</summary>
    public static ConsumerTier FromHttpContext(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true && ctx.User.IsInRole(AuthRoles.Consumer))
        {
            var t = ctx.User.FindFirst("tier")?.Value;
            return Enum.TryParse<ConsumerTier>(t, true, out var tier) ? tier : ConsumerTier.Free;
        }

        return ConsumerTier.Free;
    }

    /// <summary>Enterprise autenticado no debe verse a sí mismo en descubrimiento (solo competencia).</summary>
    public static Guid? ExcludeOwnCafeteriaId(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated != true || !ctx.User.IsInRole(AuthRoles.Enterprise))
            return null;

        var s = ctx.User.FindFirst("cafeteria_id")?.Value;
        return Guid.TryParse(s, out var id) ? id : null;
    }
}
