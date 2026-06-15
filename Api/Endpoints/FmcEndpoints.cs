using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Constants;

using Fmc.Application.Configuration;
using Microsoft.Extensions.Options;

namespace Fmc.Api.Endpoints;

public static class FmcEndpoints
{
    public static WebApplication MapFmcEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth").RequireRateLimiting("auth");

        auth.MapPost("/consumer/register", async (ConsumerRegisterRequest body, IConsumerAuthService svc, IOptions<DemoOptions> demo, CancellationToken ct) =>
            {
                if (!demo.Value.AllowRegistration)
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Registro deshabilitado",
                        detail: "Demo pública: usá las cuentas seed documentadas en /demo.");

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

        auth.MapPost("/enterprise/register", async (EnterpriseRegisterRequest body, IEnterpriseAuthService svc, IOptions<DemoOptions> demo, CancellationToken ct) =>
            {
                if (!demo.Value.AllowRegistration)
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Registro deshabilitado",
                        detail: "Demo pública: usá las cuentas seed documentadas en /demo.");

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

        discovery.MapGet("/{cafeteriaId:guid}/photos", async (
                Guid cafeteriaId,
                ICafeteriaPhotoService photos,
                CancellationToken ct) =>
            {
                var result = await photos.ListAsync(cafeteriaId, ct);
                return Results.Ok(result);
            })
            .AllowAnonymous();

        discovery.MapPost("/{cafeteriaId:guid}/photos", async (
                Guid cafeteriaId,
                IFormFile file,
                HttpContext http,
                ICafeteriaPhotoService photos,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                    throw new ArgumentException("Archivo vacío.");

                var authorId = http.User.RequireUserId();
                var authorRole = http.User.RequireAuthorRole();
                await using var stream = file.OpenReadStream();
                var dto = await photos.UploadAsync(
                    cafeteriaId,
                    authorId,
                    authorRole,
                    stream,
                    file.ContentType,
                    file.Length,
                    ct);
                return Results.Created($"/api/cafeterias/{cafeteriaId}/photos/{dto.Id}", dto);
            })
            .RequireAuthorization()
            .RequireRateLimiting("upload")
            .DisableAntiforgery();

        discovery.MapDelete("/{cafeteriaId:guid}/photos/{photoId:guid}", async (
                Guid cafeteriaId,
                Guid photoId,
                HttpContext http,
                ICafeteriaPhotoService photos,
                CancellationToken ct) =>
            {
                var authorId = http.User.RequireUserId();
                var authorRole = http.User.RequireAuthorRole();
                await photos.DeleteAsync(cafeteriaId, photoId, authorId, authorRole, ct);
                return Results.NoContent();
            })
            .RequireAuthorization();

        discovery.MapGet("/{cafeteriaId:guid}/reviews", async (
                Guid cafeteriaId,
                ICafeteriaReviewService reviews,
                CancellationToken ct) =>
            {
                var result = await reviews.ListAsync(cafeteriaId, ct);
                return Results.Ok(result);
            })
            .AllowAnonymous();

        discovery.MapGet("/{cafeteriaId:guid}/coupons", async (
                Guid cafeteriaId,
                ICafeteriaCouponService coupons,
                HttpContext http,
                CancellationToken ct) =>
            {
                ConsumerTier? viewerTier = null;
                if (http.User.Identity?.IsAuthenticated == true
                    && http.User.IsInRole(AuthRoles.Consumer))
                {
                    viewerTier = DiscoveryTierResolver.FromHttpContext(http);
                }

                var result = await coupons.GetAvailableAsync(cafeteriaId, viewerTier, ct);
                return Results.Ok(result);
            })
            .AllowAnonymous();

        discovery.MapPost("/{cafeteriaId:guid}/reviews", async (
                Guid cafeteriaId,
                CafeteriaReviewCreateRequest body,
                HttpContext http,
                ICafeteriaReviewService reviews,
                CancellationToken ct) =>
            {
                var authorId = http.User.RequireUserId();
                var authorRole = http.User.RequireAuthorRole();
                var dto = await reviews.CreateOrUpdateAsync(cafeteriaId, authorId, authorRole, body, ct);
                return Results.Ok(dto);
            })
            .RequireAuthorization();

        discovery.MapPut("/{cafeteriaId:guid}/reviews/{reviewId:guid}", async (
                Guid cafeteriaId,
                Guid reviewId,
                CafeteriaReviewUpdateRequest body,
                HttpContext http,
                ICafeteriaReviewService reviews,
                CancellationToken ct) =>
            {
                var authorId = http.User.RequireUserId();
                var authorRole = http.User.RequireAuthorRole();
                var dto = await reviews.UpdateAsync(cafeteriaId, reviewId, authorId, authorRole, body, ct);
                return Results.Ok(dto);
            })
            .RequireAuthorization();

        discovery.MapDelete("/{cafeteriaId:guid}/reviews/{reviewId:guid}", async (
                Guid cafeteriaId,
                Guid reviewId,
                HttpContext http,
                ICafeteriaReviewService reviews,
                CancellationToken ct) =>
            {
                var authorId = http.User.RequireUserId();
                var authorRole = http.User.RequireAuthorRole();
                await reviews.DeleteAsync(cafeteriaId, reviewId, authorId, authorRole, ct);
                return Results.NoContent();
            })
            .RequireAuthorization();

        discovery.MapPost("/{cafeteriaId:guid}/reviews/{reviewId:guid}/photo", async (
                Guid cafeteriaId,
                Guid reviewId,
                IFormFile file,
                HttpContext http,
                ICafeteriaReviewService reviews,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                    throw new ArgumentException("Archivo vacío.");

                var authorId = http.User.RequireUserId();
                var authorRole = http.User.RequireAuthorRole();
                await using var stream = file.OpenReadStream();
                var dto = await reviews.UploadPhotoAsync(
                    cafeteriaId,
                    reviewId,
                    authorId,
                    authorRole,
                    stream,
                    file.ContentType,
                    file.Length,
                    ct);
                return Results.Ok(dto);
            })
            .RequireAuthorization()
            .RequireRateLimiting("upload")
            .DisableAntiforgery();

        discovery.MapDelete("/{cafeteriaId:guid}/reviews/{reviewId:guid}/photo", async (
                Guid cafeteriaId,
                Guid reviewId,
                HttpContext http,
                ICafeteriaReviewService reviews,
                CancellationToken ct) =>
            {
                var authorId = http.User.RequireUserId();
                var authorRole = http.User.RequireAuthorRole();
                await reviews.DeletePhotoAsync(cafeteriaId, reviewId, authorId, authorRole, ct);
                return Results.NoContent();
            })
            .RequireAuthorization();

        var consumer = app.MapGroup("/api/consumer").RequireAuthorization(policy => policy.RequireRole(AuthRoles.Consumer)).WithTags("Cliente");

        consumer.MapGet("/me", async (HttpContext http, IConsumerProfileService profiles, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            var dto = await profiles.GetProfileAsync(id, ct);
            return Results.Ok(dto);
        });

        consumer.MapPut("/me", async (
                ConsumerProfileUpdateRequest body,
                HttpContext http,
                IConsumerProfileService profiles,
                CancellationToken ct) =>
            {
                var id = http.User.RequireUserId();
                var dto = await profiles.UpdateProfileAsync(id, body, ct);
                return Results.Ok(dto);
            });

        consumer.MapPost("/me/avatar", async (
                IFormFile file,
                HttpContext http,
                IConsumerProfileService profiles,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                    throw new ArgumentException("Archivo vacío.");

                var id = http.User.RequireUserId();
                await using var stream = file.OpenReadStream();
                var dto = await profiles.UploadAvatarAsync(id, stream, file.ContentType, file.Length, ct);
                return Results.Ok(dto);
            })
            .RequireRateLimiting("upload")
            .DisableAntiforgery();

        consumer.MapDelete("/me/avatar", async (HttpContext http, IConsumerProfileService profiles, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            var dto = await profiles.DeleteAvatarAsync(id, ct);
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

        consumer.MapGet("/me/favorites", async (HttpContext http, IConsumerFavoriteService favorites, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            return Results.Ok(await favorites.ListAsync(id, ct));
        });

        consumer.MapGet("/me/favorites/ids", async (HttpContext http, IConsumerFavoriteService favorites, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            return Results.Ok(await favorites.ListIdsAsync(id, ct));
        });

        consumer.MapPut("/me/favorites/sync", async (
                IReadOnlyList<Guid> body,
                HttpContext http,
                IConsumerFavoriteService favorites,
                CancellationToken ct) =>
            {
                var id = http.User.RequireUserId();
                return Results.Ok(await favorites.SyncAsync(id, body, ct));
            });

        consumer.MapPut("/me/favorites/{cafeteriaId:guid}", async (
                Guid cafeteriaId,
                HttpContext http,
                IConsumerFavoriteService favorites,
                CancellationToken ct) =>
            {
                var id = http.User.RequireUserId();
                await favorites.AddAsync(id, cafeteriaId, ct);
                return Results.NoContent();
            });

        consumer.MapDelete("/me/favorites/{cafeteriaId:guid}", async (
                Guid cafeteriaId,
                HttpContext http,
                IConsumerFavoriteService favorites,
                CancellationToken ct) =>
            {
                var id = http.User.RequireUserId();
                await favorites.RemoveAsync(id, cafeteriaId, ct);
                return Results.NoContent();
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

        enterprise.MapPost("/me/avatar", async (
                IFormFile file,
                HttpContext http,
                IEnterpriseCafeteriaService svc,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                    throw new ArgumentException("Archivo vacío.");

                var id = http.User.RequireUserId();
                await using var stream = file.OpenReadStream();
                var dto = await svc.UploadAvatarAsync(id, stream, file.ContentType, file.Length, ct);
                return Results.Ok(dto);
            })
            .RequireRateLimiting("upload")
            .DisableAntiforgery();

        enterprise.MapDelete("/me/avatar", async (HttpContext http, IEnterpriseCafeteriaService svc, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            var dto = await svc.DeleteAvatarAsync(id, ct);
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

        enterprise.MapGet("/me/stats", async (HttpContext http, IEnterpriseStatsService stats, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            return Results.Ok(await stats.GetMineAsync(id, ct));
        });

        enterprise.MapGet("/coupons", async (HttpContext http, IEnterpriseCouponService coupons, CancellationToken ct) =>
        {
            var id = http.User.RequireUserId();
            return Results.Ok(await coupons.ListMineAsync(id, ct));
        });

        enterprise.MapPost("/coupons", async (
                EnterpriseCouponCreateRequest body,
                HttpContext http,
                IEnterpriseCouponService coupons,
                CancellationToken ct) =>
            {
                var id = http.User.RequireUserId();
                var dto = await coupons.CreateAsync(id, body, ct);
                return Results.Created($"/api/enterprise/cafeteria/coupons/{dto.Id}", dto);
            });

        enterprise.MapDelete("/coupons/{couponId:guid}", async (
                Guid couponId,
                HttpContext http,
                IEnterpriseCouponService coupons,
                CancellationToken ct) =>
            {
                var id = http.User.RequireUserId();
                await coupons.DeleteAsync(id, couponId, ct);
                return Results.NoContent();
            });

        return app;
    }
}
