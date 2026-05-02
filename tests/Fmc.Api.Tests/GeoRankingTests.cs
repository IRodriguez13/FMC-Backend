using Fmc.Application.Services;
using Fmc.Domain.Entities;

namespace Fmc.Api.Tests;

public class GeoRankingTests
{
    [Fact]
    public void DistanceMeters_SamePoint_IsZero()
    {
        var d = GeoRanking.DistanceMeters(40.4168, -3.7038, 40.4168, -3.7038);
        Assert.Equal(0, d, precision: 3);
    }

    [Fact]
    public void DistanceMeters_ApproxOneDegreeLatitude_IsAbout111km()
    {
        var d = GeoRanking.DistanceMeters(0, 0, 1, 0);
        Assert.InRange(d, 110_500, 111_500);
    }

    [Fact]
    public void RankingBoostMeters_PremiumEnterprise_ReturnsConfiguredBoost()
    {
        var boost = GeoRanking.RankingBoostMeters(EnterpriseSubscriptionTier.Premium, 2500);
        Assert.Equal(2500, boost);
    }

    [Fact]
    public void RankingBoostMeters_StandardEnterprise_ReturnsZero()
    {
        var boost = GeoRanking.RankingBoostMeters(EnterpriseSubscriptionTier.Standard, 2500);
        Assert.Equal(0, boost);
    }

    [Fact]
    public void EffectiveSortDistance_AppliesBoost()
    {
        var effective = GeoRanking.EffectiveSortDistanceMeters(3000, 2500);
        Assert.Equal(500, effective, precision: 3);
    }

    [Fact]
    public void LocationShouldFailValidation()
    {
        Assert.False(LocationValidation.IsValidLocation(40.4168, -180.0001));   
    }
}
