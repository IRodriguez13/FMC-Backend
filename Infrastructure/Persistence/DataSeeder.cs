using Fmc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedIfEmptyAsync(AppDbContext db)
    {
        if (await db.EnterpriseUsers.AnyAsync())
            return;

        const string plainPassword = "SeedPass-123";
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        // GUID fijos para que cualquier máquina con BD vacía obtenga los mismos IDs (útil para pruebas y documentación).
        var cafePremiumId = Guid.Parse("a1111111-1111-4111-8111-111111111101");
        var enterprisePremiumId = Guid.Parse("a1111111-1111-4111-8111-111111111201");
        var cafeStandardId = Guid.Parse("a2222222-2222-4222-8222-222222221101");
        var enterpriseStandardId = Guid.Parse("a2222222-2222-4222-8222-222222221201");
        var consumerFreeId = Guid.Parse("b3333333-3333-4333-8333-333333333301");
        var consumerPremiumId = Guid.Parse("b3333333-3333-4333-8333-333333333302");

        var cafePremium = new Cafeteria
        {
            Id = cafePremiumId,
            Name = "FMC Seed — Enterprise Premium",
            Description = "Demo: cuenta Enterprise Premium (mayor ponderación en listados).",
            Address = "Madrid (demo)",
            Latitude = 40.4169,
            Longitude = -3.7039,
            ListingActive = true,
            DiscountPercent = 15,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var enterprisePremium = new EnterpriseUser
        {
            Id = enterprisePremiumId,
            Email = "enterprise-premium@seed.fmc",
            PasswordHash = hash,
            CafeteriaId = cafePremiumId,
            SubscriptionTier = EnterpriseSubscriptionTier.Premium,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var cafeStandard = new Cafeteria
        {
            Id = cafeStandardId,
            Name = "FMC Seed — Enterprise Standard",
            Description = "Demo: cuenta Enterprise Standard (solo orden por distancia real).",
            Address = "Madrid (demo)",
            Latitude = 40.4174,
            Longitude = -3.7044,
            ListingActive = true,
            DiscountPercent = 8,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var enterpriseStandard = new EnterpriseUser
        {
            Id = enterpriseStandardId,
            Email = "enterprise-standard@seed.fmc",
            PasswordHash = hash,
            CafeteriaId = cafeStandardId,
            SubscriptionTier = EnterpriseSubscriptionTier.Standard,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var consumerFree = new ConsumerUser
        {
            Id = consumerFreeId,
            Email = "consumidor@seed.fmc",
            PasswordHash = hash,
            Tier = ConsumerTier.Free,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var consumerPremium = new ConsumerUser
        {
            Id = consumerPremiumId,
            Email = "consumidor-premium@seed.fmc",
            PasswordHash = hash,
            Tier = ConsumerTier.Premium,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Cafeterias.AddRange(cafePremium, cafeStandard);
        db.EnterpriseUsers.AddRange(enterprisePremium, enterpriseStandard);
        db.ConsumerUsers.AddRange(consumerFree, consumerPremium);
        await db.SaveChangesAsync();
    }
}
