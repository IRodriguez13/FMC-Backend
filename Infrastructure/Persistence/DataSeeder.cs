using Fmc.Application.Configuration;
using Fmc.Domain.Constants;
using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence;

/// <summary>
/// Datos demo versionados en Git. <see cref="EnsureCabaDemoAsync"/> es idempotente:
/// corrige seeds viejos (Madrid) y añade locales/enterprise faltantes en CABA.
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
        EnsureCabaDemoAsync(db, media);

    public static async Task EnsureCabaDemoAsync(AppDbContext db, MediaOptions? media = null)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
        var now = DateTimeOffset.UtcNow;

        await UpsertCafeteriaAsync(db, CafePremiumId,
            "FMC Seed — Palermo (Premium)",
            "Demo Enterprise Premium en Palermo, CABA.",
            "Thames 1500, Palermo, CABA",
            -34.5875, -58.4250, 15, now);

        await UpsertCafeteriaAsync(db, CafeStandardId,
            "FMC Seed — San Telmo (Standard)",
            "Demo Enterprise Standard en San Telmo, CABA.",
            "Defensa 800, San Telmo, CABA",
            -34.6210, -58.3730, 8, now);

        await UpsertCafeteriaAsync(db, CafeRecoletaId,
            "FMC Seed — Recoleta (Premium)",
            "Demo segundo local Premium en Recoleta.",
            "Av. Callao 1500, Recoleta, CABA",
            -34.5950, -58.3920, 12, now);

        await UpsertCafeteriaAsync(db, CafeCaballitoId,
            "FMC Seed — Caballito (Standard)",
            "Demo segundo local Standard en Caballito.",
            "Río de Janeiro 600, Caballito, CABA",
            -34.6195, -58.4320, 10, now);

        await UpsertEnterpriseAsync(db, EnterprisePremiumId, "enterprise-premium@seed.fmc",
            CafePremiumId, EnterpriseSubscriptionTier.Premium, hash, now);
        await UpsertEnterpriseAsync(db, EnterpriseStandardId, "enterprise-standard@seed.fmc",
            CafeStandardId, EnterpriseSubscriptionTier.Standard, hash, now);
        await UpsertEnterpriseAsync(db, EnterpriseRecoletaId, "enterprise-recoleta@seed.fmc",
            CafeRecoletaId, EnterpriseSubscriptionTier.Premium, hash, now);
        await UpsertEnterpriseAsync(db, EnterpriseCaballitoId, "enterprise-caballito@seed.fmc",
            CafeCaballitoId, EnterpriseSubscriptionTier.Standard, hash, now);

        await UpsertConsumerAsync(db, ConsumerFreeId, "consumidor@seed.fmc", ConsumerTier.Free, hash, now);
        await UpsertConsumerAsync(db, ConsumerPremiumId, "consumidor-premium@seed.fmc", ConsumerTier.Premium, hash, now);

        await db.SaveChangesAsync();

        await EnsureMediaDemoAsync(db, media ?? new MediaOptions(), now);
    }

    private static async Task EnsureMediaDemoAsync(AppDbContext db, MediaOptions media, DateTimeOffset now)
    {
        var uploadRoot = media.UploadRoot;
        var seedAssetsRoot = SeedImageFiles.ResolveSeedAssetsRoot(uploadRoot);

        foreach (var seed in SeedPhotos)
        {
            SeedImageFiles.EnsureOnDisk(uploadRoot, seed.StorageKey, seedAssetsRoot);
            await UpsertPhotoAsync(db, seed.PhotoId, seed.CafeteriaId, seed.StorageKey,
                seed.AuthorUserId, seed.AuthorRole, now.AddDays(-seed.DaysAgo));
        }

        SeedImageFiles.CleanupLegacySeedFiles(uploadRoot);

        foreach (var seed in SeedReviews)
        {
            await UpsertReviewAsync(db, seed.ReviewId, seed.CafeteriaId, seed.AuthorUserId, seed.AuthorRole,
                seed.Rating, seed.Text, now.AddDays(-seed.DaysAgo));
        }

        await db.SaveChangesAsync();
    }

    private static readonly (Guid PhotoId, Guid CafeteriaId, string StorageKey, Guid AuthorUserId, string AuthorRole, int DaysAgo)[] SeedPhotos =
    [
        (Guid.Parse("c1111111-1111-4111-8111-111111111101"), CafePremiumId, "seed-palermo-interior.jpg", ConsumerPremiumId, AuthRoles.Consumer, 12),
        (Guid.Parse("c1111111-1111-4111-8111-111111111102"), CafePremiumId, "seed-palermo-barra.jpg", EnterprisePremiumId, AuthRoles.Enterprise, 8),
        (Guid.Parse("c2222222-2222-4222-8222-222222221101"), CafeStandardId, "seed-san-telmo-patio.jpg", ConsumerFreeId, AuthRoles.Consumer, 20),
        (Guid.Parse("c3333333-3333-4333-8333-333333331101"), CafeRecoletaId, "seed-recoleta-frente.jpg", ConsumerPremiumId, AuthRoles.Consumer, 5),
        (Guid.Parse("c3333333-3333-4333-8333-333333331102"), CafeRecoletaId, "seed-recoleta-detalle.jpg", EnterpriseRecoletaId, AuthRoles.Enterprise, 3),
        (Guid.Parse("c4444444-4444-4444-8444-444444441101"), CafeCaballitoId, "seed-caballito-local.jpg", EnterpriseStandardId, AuthRoles.Enterprise, 15),
        (Guid.Parse("c4444444-4444-4444-8444-444444441102"), CafeCaballitoId, "seed-caballito-visita.jpg", ConsumerFreeId, AuthRoles.Consumer, 2),
    ];

    private static readonly (Guid ReviewId, Guid CafeteriaId, Guid AuthorUserId, string AuthorRole, int Rating, string Text, int DaysAgo)[] SeedReviews =
    [
        (Guid.Parse("d1111111-1111-4111-8111-111111111101"), CafePremiumId, ConsumerFreeId, AuthRoles.Consumer, 5,
            "Ambiente increíble en Palermo. Ideal para trabajar con notebook un rato.", 14),
        (Guid.Parse("d1111111-1111-4111-8111-111111111102"), CafePremiumId, ConsumerPremiumId, AuthRoles.Consumer, 4,
            "Muy buen flat white. Volvería un sábado a la tarde.", 7),
        (Guid.Parse("d1111111-1111-4111-8111-111111111103"), CafePremiumId, EnterpriseStandardId, AuthRoles.Enterprise, 4,
            "Buen punto para reuniones informales; wifi estable.", 4),
        (Guid.Parse("d2222222-2222-4222-8222-222222221101"), CafeStandardId, ConsumerPremiumId, AuthRoles.Consumer, 3,
            "San Telmo clásico. Un poco ruidoso al mediodía pero auténtico.", 18),
        (Guid.Parse("d2222222-2222-4222-8222-222222221102"), CafeStandardId, EnterpriseRecoletaId, AuthRoles.Enterprise, 5,
            "Medialunas excelentes. Recomendado si estás de paseo por la feria.", 9),
        (Guid.Parse("d3333333-3333-4333-8333-333333331101"), CafeRecoletaId, ConsumerFreeId, AuthRoles.Consumer, 4,
            "Muy lindo local en Recoleta, atención amable.", 6),
        (Guid.Parse("d3333333-3333-4333-8333-333333331102"), CafeRecoletaId, EnterpriseCaballitoId, AuthRoles.Enterprise, 4,
            "Buen espresso y mesas cómodas cerca del Bajo.", 11),
        (Guid.Parse("d4444444-4444-4444-8444-444444441101"), CafeCaballitoId, ConsumerPremiumId, AuthRoles.Consumer, 5,
            "Caballito necesitaba un café así. Precios razonables.", 3),
        (Guid.Parse("d4444444-4444-4444-8444-444444441102"), CafeCaballitoId, EnterprisePremiumId, AuthRoles.Enterprise, 3,
            "Correcto para un café rápido antes del laburo.", 1),
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
        }
    }
}
