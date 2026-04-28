namespace Fmc.Api.Services;

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
}
