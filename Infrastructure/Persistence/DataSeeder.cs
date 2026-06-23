using Fmc.Application.Configuration;
using Fmc.Application.Services;
using Fmc.Domain.Constants;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence;

/// <summary>
/// Datos demo versionados en Git. <see cref="EnsureCabaCatalogAsync"/> es idempotente:
/// 20 cafeterías en CABA con fotos desde <c>Api/SeedAssets</c>.
/// </summary>
public static class DataSeeder
{
    public const string DemoPassword = "SeedPass-123";

    public static readonly Guid CafePremiumId = Guid.Parse("a1111111-1111-4111-8111-111111111101");
    public static readonly Guid EnterprisePremiumId = Guid.Parse("a1111111-1111-4111-8111-111111111201");
    public static readonly Guid CafeStandardId = Guid.Parse("a2222222-2222-4222-8222-222222221101");
    public static readonly Guid EnterpriseStandardId = Guid.Parse("a2222222-2222-4222-8222-222222221201");
    public static readonly Guid CafeRecoletaId = Guid.Parse("a3333333-3333-4333-8333-333333331101");
    public static readonly Guid EnterpriseRecoletaId = Guid.Parse("a3333333-3333-4333-8333-333333331201");
    public static readonly Guid CafeCaballitoId = Guid.Parse("a4444444-4444-4444-8444-444444441101");
    public static readonly Guid EnterpriseCaballitoId = Guid.Parse("a4444444-4444-4444-8444-444444441201");
    public static readonly Guid ConsumerFreeId = Guid.Parse("b3333333-3333-4333-8333-333333333301");
    public static readonly Guid ConsumerPremiumId = Guid.Parse("b3333333-3333-4333-8333-333333333302");

    public static Task SeedIfEmptyAsync(AppDbContext db, MediaOptions? media = null) =>
        EnsureCabaCatalogAsync(db, media);

    public static Task EnsureCabaDemoAsync(AppDbContext db, MediaOptions? media = null) =>
        EnsureCabaCatalogAsync(db, media);

    /// <returns>Cantidad de cafeterías del catálogo seed.</returns>
    public static async Task<int> EnsureCabaCatalogAsync(AppDbContext db, MediaOptions? media = null)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
        var now = DateTimeOffset.UtcNow;
        var options = media ?? new MediaOptions();

        foreach (var cafe in CabaCatalogSeed.Cafes)
        {
            await UpsertCafeteriaAsync(db, cafe.CafeId, cafe.Name, cafe.Description, cafe.Address,
                cafe.Latitude, cafe.Longitude, cafe.DiscountPercent, now);
            await UpsertEnterpriseAsync(db, cafe.EnterpriseId, cafe.EnterpriseEmail, cafe.CafeId,
                cafe.Tier, hash, now);
        }

        await UpsertConsumerAsync(db, ConsumerFreeId, "consumidor@seed.fmc", ConsumerTier.Free, hash, now);
        await UpsertConsumerAsync(db, ConsumerPremiumId, "consumidor-premium@seed.fmc", ConsumerTier.Premium, hash, now);

        await db.SaveChangesAsync();

        await EnsureSeedCouponsAsync(db, now);
        await EnsureCatalogMediaAsync(db, options, now);
        await EnsureCatalogReviewsAsync(db, options, now);

        return CabaCatalogSeed.Count;
    }

    private static async Task EnsureSeedCouponsAsync(AppDbContext db, DateTimeOffset now)
    {
        var (weekStart, weekEnd) = CouponWeek.CurrentBounds(now);

        var coupons = new (Guid Id, Guid CafeteriaId, string Code, string Title)[]
        {
            (Guid.Parse("e1111111-1111-4111-8111-111111111101"), CafePremiumId, "PALERMO-2X1", "2x1 en cappuccino"),
            (Guid.Parse("e0000009-1111-4111-8111-111111111101"), CabaCatalogSeed.Cafes[8].CafeId, "PM-15OFF", "15% en take away"),
            (Guid.Parse("e0000011-1111-4111-8111-111111111101"), CabaCatalogSeed.Cafes[10].CafeId, "COLEG-FILTER", "Filter gratis con pastelería"),
        };

        foreach (var (id, cafeteriaId, code, title) in coupons)
        {
            var exists = await db.EnterpriseCoupons
                .AnyAsync(c => c.CafeteriaId == cafeteriaId && c.Code == code);
            if (exists) continue;

            db.EnterpriseCoupons.Add(new EnterpriseCoupon
            {
                Id = id,
                CafeteriaId = cafeteriaId,
                Kind = CouponKind.TwoForOne,
                Title = title,
                Description = "Cupón demo seed — semana actual (AR).",
                Code = code,
                ValidFrom = weekStart,
                ValidUntil = weekEnd,
                IsActive = true,
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureCatalogMediaAsync(AppDbContext db, MediaOptions media, DateTimeOffset now)
    {
        var uploadRoot = media.UploadRoot;
        var seedAssetsRoot = SeedImageFiles.ResolveSeedAssetsRoot(uploadRoot);

        var legacyConsumerPhotos = await db.CafeteriaPhotos
            .Where(p => p.AuthorRole == AuthRoles.Consumer)
            .ToListAsync();
        if (legacyConsumerPhotos.Count > 0)
            db.CafeteriaPhotos.RemoveRange(legacyConsumerPhotos);

        foreach (var asset in CabaCatalogSeed.PhotoAssets)
            SeedImageFiles.EnsureOnDisk(uploadRoot, asset, seedAssetsRoot);

        foreach (var cafe in CabaCatalogSeed.Cafes)
        {
            SeedImageFiles.EnsureOnDisk(uploadRoot, cafe.PhotoStorageKey, seedAssetsRoot);
            await UpsertPhotoAsync(db, cafe.PhotoId, cafe.CafeId, cafe.PhotoStorageKey,
                cafe.EnterpriseId, AuthRoles.Enterprise, now.AddDays(-(cafe.CafeId.GetHashCode() & 15) - 1));

            if (cafe.AvatarStorageKey is not null)
            {
                SeedImageFiles.EnsureOnDisk(uploadRoot, cafe.AvatarStorageKey, seedAssetsRoot);
                var user = await db.EnterpriseUsers.FindAsync(cafe.EnterpriseId);
                if (user is not null)
                    user.AvatarStorageKey = cafe.AvatarStorageKey;
            }
        }

        SeedImageFiles.CleanupLegacySeedFiles(uploadRoot);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureCatalogReviewsAsync(AppDbContext db, MediaOptions media, DateTimeOffset now)
    {
        var uploadRoot = media.UploadRoot;
        var seedAssetsRoot = SeedImageFiles.ResolveSeedAssetsRoot(uploadRoot);

        foreach (var seed in CoreReviews)
        {
            if (seed.PhotoStorageKey is not null)
                SeedImageFiles.EnsureOnDisk(uploadRoot, seed.PhotoStorageKey, seedAssetsRoot);
            await UpsertReviewAsync(db, seed.ReviewId, seed.CafeteriaId, seed.AuthorUserId, seed.AuthorRole,
                seed.Rating, seed.Text, seed.PhotoStorageKey, now.AddDays(-seed.DaysAgo));
        }

        var catalogIndex = 0;
        foreach (var cafe in CabaCatalogSeed.Cafes.Skip(4))
        {
            catalogIndex++;
            var authorId = catalogIndex % 2 == 0 ? ConsumerFreeId : ConsumerPremiumId;
            var reviewId = Guid.Parse($"d{catalogIndex + 4:D7}-1111-4111-8111-111111111101");
            var photoKey = catalogIndex % 3 == 0
                ? CabaCatalogSeed.PhotoAssets[catalogIndex % CabaCatalogSeed.PhotoAssets.Length]
                : null;
            if (photoKey is not null)
                SeedImageFiles.EnsureOnDisk(uploadRoot, photoKey, seedAssetsRoot);
            await UpsertReviewAsync(db, reviewId, cafe.CafeId, authorId, AuthRoles.Consumer,
                3 + (catalogIndex % 3), $"Muy buen café en {cafe.Name.Split('—').Last().Trim().TrimEnd(')')}. Recomendado.",
                photoKey, now.AddDays(-catalogIndex));
        }

        await db.SaveChangesAsync();
    }

    private static readonly (Guid ReviewId, Guid CafeteriaId, Guid AuthorUserId, string AuthorRole, int Rating, string Text, string? PhotoStorageKey, int DaysAgo)[] CoreReviews =
    [
        (Guid.Parse("d1111111-1111-4111-8111-111111111101"), CafePremiumId, ConsumerFreeId, AuthRoles.Consumer, 5,
            "Ambiente increíble en Palermo. Ideal para trabajar con notebook un rato.", "seed-palermo-interior.jpg", 14),
        (Guid.Parse("d1111111-1111-4111-8111-111111111102"), CafePremiumId, ConsumerPremiumId, AuthRoles.Consumer, 4,
            "Muy buen flat white. Volvería un sábado a la tarde.", "seed-caballito-visita.jpg", 7),
        (Guid.Parse("d1111111-1111-4111-8111-111111111103"), CafePremiumId, EnterpriseStandardId, AuthRoles.Enterprise, 4,
            "Buen punto para reuniones informales; wifi estable.", "seed-recoleta-frente.jpg", 4),
        (Guid.Parse("d2222222-2222-4222-8222-222222221101"), CafeStandardId, ConsumerPremiumId, AuthRoles.Consumer, 3,
            "San Telmo clásico. Un poco ruidoso al mediodía pero auténtico.", "seed-recoleta-frente.jpg", 18),
        (Guid.Parse("d2222222-2222-4222-8222-222222221102"), CafeStandardId, EnterpriseRecoletaId, AuthRoles.Enterprise, 5,
            "Medialunas excelentes. Recomendado si estás de paseo por la feria.", null, 9),
        (Guid.Parse("d3333333-3333-4333-8333-333333331101"), CafeRecoletaId, ConsumerFreeId, AuthRoles.Consumer, 4,
            "Muy lindo local en Recoleta, atención amable.", "seed-palermo-interior.jpg", 6),
        (Guid.Parse("d3333333-3333-4333-8333-333333331102"), CafeRecoletaId, EnterpriseCaballitoId, AuthRoles.Enterprise, 4,
            "Buen espresso y mesas cómodas cerca del Bajo.", null, 11),
        (Guid.Parse("d4444444-4444-4444-8444-444444441101"), CafeCaballitoId, ConsumerPremiumId, AuthRoles.Consumer, 5,
            "Caballito necesitaba un café así. Precios razonables.", "seed-caballito-visita.jpg", 3),
        (Guid.Parse("d4444444-4444-4444-8444-444444441102"), CafeCaballitoId, EnterprisePremiumId, AuthRoles.Enterprise, 3,
            "Correcto para un café rápido antes del laburo.", null, 1),
    ];

    private static async Task UpsertPhotoAsync(
        AppDbContext db,
        Guid id,
        Guid cafeteriaId,
        string storageKey,
        Guid authorUserId,
        string authorRole,
        DateTimeOffset createdAt)
    {
        var photo = await db.CafeteriaPhotos.FindAsync(id);
        if (photo is null)
        {
            db.CafeteriaPhotos.Add(new CafeteriaPhoto
            {
                Id = id,
                CafeteriaId = cafeteriaId,
                StorageKey = storageKey,
                ContentType = "image/jpeg",
                AuthorUserId = authorUserId,
                AuthorRole = authorRole,
                CreatedAt = createdAt,
            });
            return;
        }

        photo.CafeteriaId = cafeteriaId;
        photo.StorageKey = storageKey;
        photo.ContentType = "image/jpeg";
        photo.AuthorUserId = authorUserId;
        photo.AuthorRole = authorRole;
    }

    private static async Task UpsertReviewAsync(
        AppDbContext db,
        Guid id,
        Guid cafeteriaId,
        Guid authorUserId,
        string authorRole,
        int rating,
        string text,
        string? photoStorageKey,
        DateTimeOffset createdAt)
    {
        var review = await db.CafeteriaReviews.FindAsync(id);
        if (review is null)
        {
            db.CafeteriaReviews.Add(new CafeteriaReview
            {
                Id = id,
                CafeteriaId = cafeteriaId,
                AuthorUserId = authorUserId,
                AuthorRole = authorRole,
                Rating = rating,
                Text = text,
                PhotoStorageKey = photoStorageKey,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            });
            return;
        }

        review.CafeteriaId = cafeteriaId;
        review.AuthorUserId = authorUserId;
        review.AuthorRole = authorRole;
        review.Rating = rating;
        review.Text = text;
        review.PhotoStorageKey = photoStorageKey;
        review.UpdatedAt = createdAt;
    }

    private static async Task UpsertCafeteriaAsync(
        AppDbContext db,
        Guid id,
        string name,
        string description,
        string address,
        double latitude,
        double longitude,
        int discountPercent,
        DateTimeOffset updatedAt)
    {
        var cafe = await db.Cafeterias.FindAsync(id);
        if (cafe is null)
        {
            db.Cafeterias.Add(new Cafeteria
            {
                Id = id,
                Name = name,
                Description = description,
                Address = address,
                Latitude = latitude,
                Longitude = longitude,
                ListingActive = true,
                DiscountPercent = discountPercent,
                UpdatedAt = updatedAt,
            });
            return;
        }

        cafe.Name = name;
        cafe.Description = description;
        cafe.Address = address;
        cafe.Latitude = latitude;
        cafe.Longitude = longitude;
        cafe.DiscountPercent = discountPercent;
        cafe.ListingActive = CabaServiceArea.Contains(latitude, longitude);
        cafe.UpdatedAt = updatedAt;
    }

    private static async Task UpsertEnterpriseAsync(
        AppDbContext db,
        Guid id,
        string email,
        Guid cafeteriaId,
        EnterpriseSubscriptionTier tier,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.EnterpriseUsers.FindAsync(id)
            ?? await db.EnterpriseUsers.FirstOrDefaultAsync(e => e.Email == normalized);

        if (user is null)
        {
            db.EnterpriseUsers.Add(new EnterpriseUser
            {
                Id = id,
                Email = normalized,
                PasswordHash = passwordHash,
                CafeteriaId = cafeteriaId,
                SubscriptionTier = tier,
                CreatedAt = createdAt,
            });
            return;
        }

        user.Email = normalized;
        user.CafeteriaId = cafeteriaId;
        user.SubscriptionTier = tier;
        user.PasswordHash = passwordHash;
    }

    private static async Task UpsertConsumerAsync(
        AppDbContext db,
        Guid id,
        string email,
        ConsumerTier tier,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.ConsumerUsers.FindAsync(id)
            ?? await db.ConsumerUsers.FirstOrDefaultAsync(c => c.Email == normalized);

        if (user is null)
        {
            db.ConsumerUsers.Add(new ConsumerUser
            {
                Id = id,
                Email = normalized,
                PasswordHash = passwordHash,
                Tier = tier,
                CreatedAt = createdAt,
            });
        }
        else
        {
            user.Email = normalized;
            user.Tier = tier;
            user.PasswordHash = passwordHash;
        }
    }
}
