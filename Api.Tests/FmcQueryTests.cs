using System.Security.Claims;
using Fmc.Api.GraphQL;
using Fmc.Application.Contracts;
using Fmc.Application.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Fmc.Api.Tests;

public class FmcQueryTests
{
    [Fact]
    public async Task GetNearbyCafeterias_CallsDiscoveryServiceWithCorrectParameters()
    {
        // Arrange
        var mockDiscovery = new Mock<ICafeteriaDiscoveryService>();
        var mockHttpAccessor = new Mock<IHttpContextAccessor>();
        
        var httpContext = new DefaultHttpContext();
        // Simulating free tier (no authorization header)
        mockHttpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var expectedResponse = new NearbyCafeteriasResponse(
            40.4168, -3.7038, 5.0, Domain.Entities.ConsumerTier.Free, 10, Array.Empty<NearbyCafeteriaItem>());

        mockDiscovery
            .Setup(d => d.GetNearbyAsync(It.IsAny<NearbyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var query = new FmcQuery();

        // Act
        var result = await query.GetNearbyCafeterias(40.4168, -3.7038, 5.0, mockDiscovery.Object, mockHttpAccessor.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.QueryLatitude, result.QueryLatitude);
        Assert.Equal(expectedResponse.QueryLongitude, result.QueryLongitude);
        mockDiscovery.Verify(d => d.GetNearbyAsync(
            It.Is<NearbyQuery>(q => q.Latitude == 40.4168 && q.Longitude == -3.7038 && q.RadiusKm == 5.0), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetConsumerProfile_ExtractsUserIdFromClaimsAndDelegatesToProfileService()
    {
        // Arrange
        var mockProfiles = new Mock<IConsumerProfileService>();
        var mockHttpAccessor = new Mock<IHttpContextAccessor>();

        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);
        
        mockHttpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var expectedProfile = new ConsumerProfileDto(userId, "consumer@test.com", Domain.Entities.ConsumerTier.Premium);
        mockProfiles
            .Setup(p => p.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProfile);

        var query = new FmcQuery();

        // Act
        var result = await query.GetConsumerProfile(mockProfiles.Object, mockHttpAccessor.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("consumer@test.com", result.Email);
        mockProfiles.Verify(p => p.GetProfileAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyCafeteria_ExtractsUserIdFromClaimsAndDelegatesToEnterpriseService()
    {
        // Arrange
        var mockCafeterias = new Mock<IEnterpriseCafeteriaService>();
        var mockHttpAccessor = new Mock<IHttpContextAccessor>();

        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        mockHttpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var expectedCafeteria = new EnterpriseCafeteriaDto(
            Guid.NewGuid(), "Enterprise Cafe", "Desc", "Addr", 1.0, 2.0, 
            Domain.Entities.EnterpriseSubscriptionTier.Premium, true, 20);

        mockCafeterias
            .Setup(s => s.GetMineAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCafeteria);

        var query = new FmcQuery();

        // Act
        var result = await query.GetMyCafeteria(mockCafeterias.Object, mockHttpAccessor.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Enterprise Cafe", result.Name);
        mockCafeterias.Verify(s => s.GetMineAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
