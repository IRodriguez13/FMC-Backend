using Fmc.Application.Contracts.Auth;
using Fmc.Application.Contracts.Consumer;
using Fmc.Application.Contracts.Discovery;
using Fmc.Application.Contracts.Enterprise;
using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Constants;

namespace Fmc.Api.Endpoints;

public static class FmcEndpoints
{
    public static WebApplication MapFmcEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/consumer/register", async (ConsumerRegisterRequest body, IConsumerAuthService svc, CancellationToken ct) =>
            {
                var res = await svc.RegisterAsync(body, ct);
                return Results.Ok(res);
            })
            .AllowAnonymous();

        auth.MapPost("/consumer/login", async (ConsumerLoginRequest body, IConsumerAuthService svc, CancellationToken ct) =>
            {
                var res = await svc.LoginAsync(body, ct);
                return Results.Ok(res);
            })
            .AllowAnonymous();

        auth.MapPost("/enterprise/register", async (EnterpriseRegisterRequest body, IEnterpriseAuthService svc, CancellationToken ct) =>
            {
                var res = await svc.RegisterAsync(body, ct);
                return Results.Ok(res);
            })
            .AllowAnonymous();

        auth.MapPost("/enterprise/login", async (EnterpriseLoginRequest body, IEnterpriseAuthService svc, CancellationToken ct) =>
            {
                var res = await svc.LoginAsync(body, ct);
                return Results.Ok(res);
            })
            .AllowAnonymous();

        var discovery = app.MapGroup("/api/cafeterias").WithTags("Descubrimiento (mapas / GPS)");

        discovery.MapGet("/nearby", async (
                double lat,
                double lng,
                double? radiusKm,
                ICafeteriaDiscoveryService discoverySvc,
                HttpContext http,
                CancellationToken ct) =>
            {
                var tier = DiscoveryTierResolver.FromHttpContext(http);
                var query = new NearbyQuery(lat, lng, radiusKm, tier);
                var result = await discoverySvc.GetNearbyAsync(query, ct);
                return Results.Ok(result);
            })
            .AllowAnonymous();

        var consumer = app.MapGroup("/api/consumer").RequireAuthorization(policy => policy.RequireRole(AuthRoles.Consumer)).WithTags("Cliente");

        consumer.MapGet("/me", async (HttpContext http, IConsumerProfileService profiles, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            var dto = await profiles.GetProfileAsync(id, ct);
            return Results.Ok(dto);
        });

        consumer.MapPatch("/tier", async (
                ConsumerTierUpdateRequest body,
                HttpContext http,
                IConsumerProfileService profiles,
                IJwtTokenService jwt,
                CancellationToken ct) =>
            {
                var id = http.User.RequireUserId();
                var dto = await profiles.SetTierAsync(id, body.Tier, ct);
                var token = jwt.CreateConsumerToken(dto.Id, dto.Email, dto.Tier);
                return Results.Ok(new ConsumerTierPatchResponse(token, dto));
            });

        var enterprise = app.MapGroup("/api/enterprise/cafeteria").RequireAuthorization(policy => policy.RequireRole(AuthRoles.Enterprise)).WithTags("Enterprise — cafetería");

        enterprise.MapGet("/me", async (HttpContext http, IEnterpriseCafeteriaService svc, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            var dto = await svc.GetMineAsync(id, ct);
            return Results.Ok(dto);
        });

        enterprise.MapPut("/me", async (EnterpriseCafeteriaUpdateRequest body, HttpContext http, IEnterpriseCafeteriaService svc, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            var dto = await svc.UpdateAsync(id, body, ct);
            return Results.Ok(dto);
        });

        enterprise.MapPatch("/subscription-tier", async (
                EnterpriseSubscriptionTierUpdateRequest body,
                HttpContext http,
                IEnterpriseCafeteriaService svc,
                IJwtTokenService jwt,
                CancellationToken ct) =>
            {
                var id = http.User.RequireUserId();
                var r = await svc.SetEnterpriseSubscriptionTierAsync(id, body.SubscriptionTier, ct);
                var token = jwt.CreateEnterpriseToken(r.enterpriseUserId, r.email, r.cafeteriaId, r.tier);
                return Results.Ok(new AuthTokenResponse(token, AuthRoles.Enterprise, null, r.cafeteriaId, r.tier));
            });

        return app;
    }
}
