using System.Security.Claims;
using Fmc.Application.Services;
using Fmc.Domain.Constants;
using Fmc.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Fmc.Api.Tests;

public class DiscoveryTierResolverTests
{
    [Fact]
    public void FromHttpContext_Anonymous_ReturnsFree()
    {
        var ctx = new DefaultHttpContext();
        Assert.Equal(ConsumerTier.Free, DiscoveryTierResolver.FromHttpContext(ctx));
    }

    [Fact]
    public void FromHttpContext_ConsumerPremium_ReturnsPremium()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Role, AuthRoles.Consumer),
                new Claim("tier", nameof(ConsumerTier.Premium)),
            },
            authenticationType: "Bearer");

        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        Assert.Equal(ConsumerTier.Premium, DiscoveryTierResolver.FromHttpContext(ctx));
    }

    [Fact]
    public void FromHttpContext_InvalidTierClaim_FallsBackToFree()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Role, AuthRoles.Consumer),
                new Claim("tier", "not-a-tier"),
            },
            authenticationType: "Bearer");

        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        Assert.Equal(ConsumerTier.Free, DiscoveryTierResolver.FromHttpContext(ctx));
    }

    [Fact]
    public void FromHttpContext_EnterpriseRole_IsNotConsumer_ReturnsFree()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, AuthRoles.Enterprise) },
            authenticationType: "Bearer");

        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        Assert.Equal(ConsumerTier.Free, DiscoveryTierResolver.FromHttpContext(ctx));
    }
}
