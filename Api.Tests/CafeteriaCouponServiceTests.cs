using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Entities;
using Moq;

namespace Fmc.Api.Tests;

public class CafeteriaCouponServiceTests
{
    [Fact]
    public async Task GetAvailableAsync_FreeViewer_ReturnsEmpty()
    {
        var cafeId = Guid.NewGuid();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(c => c.GetByIdAsync(cafeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cafeteria { Id = cafeId, Name = "Test", DiscountPercent = 15 });

        var sut = new CafeteriaCouponService(
            cafeterias.Object,
            new Mock<IEnterpriseUserRepository>().Object,
            new Mock<IEnterpriseCouponRepository>().Object);

        var result = await sut.GetAvailableAsync(cafeId, ConsumerTier.Free);

        Assert.False(result.ViewerIsPremiumConsumer);
        Assert.Null(result.PlatformCoupon);
        Assert.Empty(result.BusinessCoupons);
    }

    [Fact]
    public async Task GetAvailableAsync_PremiumViewer_ReturnsPlatformCoupon()
    {
        var cafeId = Guid.NewGuid();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(c => c.GetByIdAsync(cafeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cafeteria { Id = cafeId, Name = "Test", DiscountPercent = 15 });

        var sut = new CafeteriaCouponService(
            cafeterias.Object,
            new Mock<IEnterpriseUserRepository>().Object,
            new Mock<IEnterpriseCouponRepository>().Object);

        var result = await sut.GetAvailableAsync(cafeId, ConsumerTier.Premium);

        Assert.True(result.ViewerIsPremiumConsumer);
        Assert.NotNull(result.PlatformCoupon);
        Assert.Equal("platform", result.PlatformCoupon!.Source);
        Assert.Equal(15, result.PlatformCoupon.DiscountPercent);
    }
}
