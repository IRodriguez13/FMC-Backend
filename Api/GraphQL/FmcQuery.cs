using Fmc.Api.Endpoints;
using Fmc.Application.Contracts;
using Fmc.Application.Services;
using Fmc.Domain.Constants;
using HotChocolate.Authorization;

namespace Fmc.Api.GraphQL;

/// <summary>
/// Root query type para el schema GraphQL de FMC.
/// Reutiliza los servicios de Application layer — sin lógica adicional.
/// </summary>
public class FmcQuery
{
    /// <summary>Cafeterías cercanas (público, tier se resuelve del JWT si está presente).</summary>
    public async Task<NearbyCafeteriasResponse> GetNearbyCafeterias(
        double lat,
        double lng,
        double? radiusKm,
        [Service] ICafeteriaDiscoveryService discovery,
        [Service] IHttpContextAccessor httpAccessor,
        CancellationToken ct)
    {
        var tier = DiscoveryTierResolver.FromHttpContext(httpAccessor.HttpContext!);
        var query = new NearbyQuery(lat, lng, radiusKm, tier);
        return await discovery.GetNearbyAsync(query, ct);
    }

    /// <summary>Perfil del consumidor autenticado.</summary>
    [Authorize(Roles = [AuthRoles.Consumer])]
    public async Task<ConsumerProfileDto> GetConsumerProfile(
        [Service] IConsumerProfileService profiles,
        [Service] IHttpContextAccessor httpAccessor,
        CancellationToken ct)
    {
        var userId = httpAccessor.HttpContext!.User.RequireUserId();
        return await profiles.GetProfileAsync(userId, ct);
    }

    /// <summary>Cafetería del enterprise autenticado.</summary>
    [Authorize(Roles = [AuthRoles.Enterprise])]
    public async Task<EnterpriseCafeteriaDto> GetMyCafeteria(
        [Service] IEnterpriseCafeteriaService cafeterias,
        [Service] IHttpContextAccessor httpAccessor,
        CancellationToken ct)
    {
        var userId = httpAccessor.HttpContext!.User.RequireUserId();
        return await cafeterias.GetMineAsync(userId, ct);
    }
}
