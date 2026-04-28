using Fmc.Api.Entities;
using Fmc.Api.Repositories;
using Fmc.Api.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace Fmc.Api.Tests;

public class CafeteriaDiscoveryServiceTests
{
    private static DiscoveryOptions DefaultDiscoveryOptions() =>
        new()
        {
            PremiumEnterpriseRankingBoostMeters = 2500,
            FreeTierMaxResults = 10,
            PremiumTierMaxResults = 50,
            FreeTierMaxRadiusKm = 5,
            PremiumTierMaxRadiusKm = 15,
        };

    private static Cafeteria CreateListedCafe(
        string name,
        double lat,
        double lng,
        EnterpriseSubscriptionTier tier,
        int discountPercent = 0)
    {
        var cid = Guid.NewGuid();
        var eid = Guid.NewGuid();
        var cafe = new Cafeteria
        {
            Id = cid,
            Name = name,
            Latitude = lat,
            Longitude = lng,
            ListingActive = true,
            DiscountPercent = discountPercent,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var eu = new EnterpriseUser
        {
            Id = eid,
            Email = $"{Guid.NewGuid():N}@test.fmc",
            PasswordHash = "x",
            CafeteriaId = cid,
            SubscriptionTier = tier,
            Cafeteria = cafe,
        };
        cafe.EnterpriseUser = eu;
        return cafe;
    }

    [Fact]
    public async Task GetNearbyAsync_OrdersPremiumEnterpriseAhead_WhenEffectiveDistanceBeatsCloserStandard()
    {
        const double userLat = 40.4168;
        const double userLng = -3.7038;

        var closeStandard = CreateListedCafe("Close", userLat + 0.0009, userLng, EnterpriseSubscriptionTier.Standard);
        var farPremium = CreateListedCafe("PremiumFar", userLat + 0.0207, userLng, EnterpriseSubscriptionTier.Premium);

        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cafeteria> { closeStandard, farPremium });

        var sut = new CafeteriaDiscoveryService(mock.Object, Options.Create(DefaultDiscoveryOptions()));

        var result = await sut.GetNearbyAsync(new NearbyQuery(userLat, userLng, RadiusKm: null, ConsumerTier.Premium));

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("PremiumFar", result.Items[0].Name);
        Assert.Equal("Close", result.Items[1].Name);
    }

    [Fact]
    public async Task GetNearbyAsync_FreeTier_CapsResultCount()
    {
        const double userLat = 40.4168;
        const double userLng = -3.7038;

        var cafes = Enumerable.Range(0, 15)
            .Select(i => CreateListedCafe($"Cafe-{i}", userLat + i * 0.00002, userLng, EnterpriseSubscriptionTier.Standard))
            .ToList();

        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cafes);

        var sut = new CafeteriaDiscoveryService(mock.Object, Options.Create(DefaultDiscoveryOptions()));

        var result = await sut.GetNearbyAsync(new NearbyQuery(userLat, userLng, RadiusKm: null, ConsumerTier.Free));

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(10, result.MaxResultsCap);
    }

    [Fact]
    public async Task GetNearbyAsync_FreeTier_ClampsRequestedRadiusToConfiguredMax()
    {
        const double userLat = 40.4168;
        const double userLng = -3.7038;

        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Cafeteria>());

        var sut = new CafeteriaDiscoveryService(mock.Object, Options.Create(DefaultDiscoveryOptions()));

        var result = await sut.GetNearbyAsync(new NearbyQuery(userLat, userLng, RadiusKm: 100, ConsumerTier.Free));

        Assert.Equal(5, result.AppliedRadiusKm);
    }

    [Fact]
    public async Task GetNearbyAsync_PremiumConsumer_SeesDiscountPercent_FreeDoesNot()
    {
        const double lat = 40.4168;
        const double lng = -3.7038;

        var cafe = CreateListedCafe("ConDescuento", lat + 0.001, lng, EnterpriseSubscriptionTier.Standard, discountPercent: 12);

        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Cafeteria> { cafe });

        var sut = new CafeteriaDiscoveryService(mock.Object, Options.Create(DefaultDiscoveryOptions()));

        var premiumResult = await sut.GetNearbyAsync(new NearbyQuery(lat, lng, RadiusKm: 50, ConsumerTier.Premium));
        var freeResult = await sut.GetNearbyAsync(new NearbyQuery(lat, lng, RadiusKm: 50, ConsumerTier.Free));

        Assert.Equal(12, premiumResult.Items[0].DiscountPercent);
        Assert.Null(freeResult.Items[0].DiscountPercent);
    }
}
