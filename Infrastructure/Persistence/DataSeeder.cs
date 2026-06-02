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

    public static Task SeedIfEmptyAsync(AppDbContext db) => EnsureCabaDemoAsync(db);

    public static async Task EnsureCabaDemoAsync(AppDbContext db)
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
