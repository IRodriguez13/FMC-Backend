using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Entities;
using Moq;

namespace Fmc.Api.Tests;

public class EnterpriseCouponServiceTests
{
    private static EnterpriseUser CreateEnterprise(EnterpriseSubscriptionTier tier = EnterpriseSubscriptionTier.Premium)
    {
        var cafeId = Guid.Parse("a1111111-1111-4111-8111-111111111101");
        return new EnterpriseUser
        {
            Id = Guid.NewGuid(),
            Email = "ent@test.fmc",
            CafeteriaId = cafeId,
            SubscriptionTier = tier,
            Cafeteria = new Cafeteria
            {
                Id = cafeId,
                Name = "Palermo Test",
            },
        };
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNotEnterprisePremium()
    {
        var eu = CreateEnterprise(EnterpriseSubscriptionTier.Standard);
        var users = new Mock<IEnterpriseUserRepository>();
        users.Setup(u => u.GetByIdAsync(eu.Id, It.IsAny<CancellationToken>())).ReturnsAsync(eu);

        var sut = new EnterpriseCouponService(users.Object, new Mock<IEnterpriseCouponRepository>().Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.CreateAsync(eu.Id, new EnterpriseCouponCreateRequest(CouponKind.Percent, 10, null, null, null, null)));
    }

    [Fact]
    public async Task CreateAsync_CreatesPercentCoupon_WhenPremium()
    {
        var eu = CreateEnterprise();
        var users = new Mock<IEnterpriseUserRepository>();
        users.Setup(u => u.GetByIdAsync(eu.Id, It.IsAny<CancellationToken>())).ReturnsAsync(eu);

        var coupons = new Mock<IEnterpriseCouponRepository>();
        coupons.Setup(c => c.CountActiveForWeekAsync(eu.CafeteriaId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        coupons.Setup(c => c.GetByCodeAsync(eu.CafeteriaId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnterpriseCoupon?)null);
        coupons.Setup(c => c.AddAsync(It.IsAny<EnterpriseCoupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnterpriseCoupon c, CancellationToken _) => c);

        var sut = new EnterpriseCouponService(users.Object, coupons.Object);
        var result = await sut.CreateAsync(eu.Id, new EnterpriseCouponCreateRequest(CouponKind.Percent, 20, null, "20% semana", null, null));

        Assert.Equal(CouponKind.Percent, result.Kind);
        Assert.Equal(20, result.DiscountPercent);
        Assert.True(result.IsActive);
    }
}
