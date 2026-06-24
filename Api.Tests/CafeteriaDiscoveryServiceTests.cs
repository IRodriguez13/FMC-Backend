using Fmc.Application.Caching;
using Fmc.Application.Configuration;
using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;

namespace Fmc.Api.Tests;

public class CafeteriaDiscoveryServiceTests
{
    private const double CabaLat = -34.6037;
    private const double CabaLng = -58.3816;

    private static DiscoveryOptions DefaultDiscoveryOptions() =>
        new()
        {
            PremiumEnterpriseRankingBoostMeters = 2500,
            FreeTierMaxResults = 10,
            PremiumTierMaxResults = 50,
            FreeTierMaxRadiusKm = 5,
            PremiumTierMaxRadiusKm = 15,
        };

    private static CafeteriaDiscoveryService CreateSut(Mock<ICafeteriaRepository> cafeteriaMock)
    {
        var photoMock = new Mock<ICafeteriaPhotoRepository>();
        photoMock
            .Setup(r => r.GetLatestStorageKeyByCafeteriaIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        var reviewMock = new Mock<ICafeteriaReviewRepository>();
        reviewMock
            .Setup(r => r.GetSummariesByCafeteriaIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, (double?, int)>());

        var storageMock = new Mock<IFileStorageService>();

        return new CafeteriaDiscoveryService(
            cafeteriaMock.Object,
            photoMock.Object,
            reviewMock.Object,
            storageMock.Object,
            new PassthroughDiscoveryReadCache(),
            Options.Create(DefaultDiscoveryOptions()));
    }

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
        const double userLat = CabaLat;
        const double userLng = CabaLng;

        var closeStandard = CreateListedCafe("Close", userLat + 0.0009, userLng, EnterpriseSubscriptionTier.Standard);
        var farPremium = CreateListedCafe("PremiumFar", userLat + 0.0207, userLng, EnterpriseSubscriptionTier.Premium);

        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cafeteria> { closeStandard, farPremium });

        var sut = CreateSut(mock);

        var result = await sut.GetNearbyAsync(new NearbyQuery(userLat, userLng, RadiusKm: null, ConsumerTier.Premium));

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("PremiumFar", result.Items[0].Name);
        Assert.Equal("Close", result.Items[1].Name);
    }

    [Fact]
    public async Task GetNearbyAsync_FreeTier_CapsResultCount()
    {
        const double userLat = CabaLat;
        const double userLng = CabaLng;

        var cafes = Enumerable.Range(0, 15)
            .Select(i => CreateListedCafe($"Cafe-{i}", userLat + i * 0.00002, userLng, EnterpriseSubscriptionTier.Standard))
            .ToList();

        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cafes);

        var sut = CreateSut(mock);

        var result = await sut.GetNearbyAsync(new NearbyQuery(userLat, userLng, RadiusKm: null, ConsumerTier.Free));

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(10, result.MaxResultsCap);
    }

    [Fact]
    public async Task GetNearbyAsync_FreeTier_ClampsRequestedRadiusToConfiguredMax()
    {
        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Cafeteria>());

        var sut = CreateSut(mock);

        var result = await sut.GetNearbyAsync(new NearbyQuery(CabaLat, CabaLng, RadiusKm: 100, ConsumerTier.Free));

        Assert.Equal(5, result.AppliedRadiusKm);
    }

    [Fact]
    public async Task GetNearbyAsync_PremiumConsumer_SeesDiscountPercent_FreeDoesNot()
    {
        var cafe = CreateListedCafe("ConDescuento", CabaLat + 0.001, CabaLng, EnterpriseSubscriptionTier.Standard, discountPercent: 12);

        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Cafeteria> { cafe });

        var sut = CreateSut(mock);

        var premiumResult = await sut.GetNearbyAsync(new NearbyQuery(CabaLat, CabaLng, RadiusKm: 50, ConsumerTier.Premium));
        var freeResult = await sut.GetNearbyAsync(new NearbyQuery(CabaLat, CabaLng, RadiusKm: 50, ConsumerTier.Free));

        Assert.Equal(12, premiumResult.Items[0].DiscountPercent);
        Assert.Null(freeResult.Items[0].DiscountPercent);
    }

    [Fact]
    public async Task GetNearbyAsync_ExcludesOwnCafeteria_WhenExcludeIdSet()
    {
        var mine = CreateListedCafe("Mine", CabaLat + 0.001, CabaLng, EnterpriseSubscriptionTier.Premium);
        var rival = CreateListedCafe("Rival", CabaLat + 0.002, CabaLng, EnterpriseSubscriptionTier.Standard);

        var mock = new Mock<ICafeteriaRepository>();
        mock.Setup(r => r.GetListedForDiscoveryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cafeteria> { mine, rival });

        var sut = CreateSut(mock);

        var result = await sut.GetNearbyAsync(
            new NearbyQuery(CabaLat, CabaLng, RadiusKm: 50, ConsumerTier.Free, mine.Id));

        Assert.Single(result.Items);
        Assert.Equal("Rival", result.Items[0].Name);
    }

    [Fact]
    public async Task GetNearbyAsync_QueryOutsideCaba_Throws()
    {
        var mock = new Mock<ICafeteriaRepository>();
        var sut = CreateSut(mock);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetNearbyAsync(new NearbyQuery(40.4168, -3.7038, RadiusKm: 5, ConsumerTier.Free)));

    }
}
