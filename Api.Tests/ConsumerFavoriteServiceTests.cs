using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Entities;
using Moq;

namespace Fmc.Api.Tests;

public class ConsumerFavoriteServiceTests
{
    private static ConsumerFavoriteService CreateSut(
        Mock<IConsumerUserRepository>? users = null,
        Mock<ICafeteriaRepository>? cafeterias = null,
        Mock<IConsumerFavoriteRepository>? favorites = null)
    {
        users ??= new Mock<IConsumerUserRepository>();
        cafeterias ??= new Mock<ICafeteriaRepository>();
        favorites ??= new Mock<IConsumerFavoriteRepository>();

        return new ConsumerFavoriteService(
            users.Object,
            cafeterias.Object,
            favorites.Object,
            new Mock<ICafeteriaPhotoRepository>().Object,
            new Mock<ICafeteriaReviewRepository>().Object,
            new Mock<IEnterpriseUserRepository>().Object,
            new Mock<IFileStorageService>().Object);
    }

    [Fact]
    public async Task AddAsync_Throws_WhenCafeteriaMissing()
    {
        var userId = Guid.NewGuid();
        var cafeId = Guid.NewGuid();
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsumerUser { Id = userId, Email = "a@b.c" });

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(c => c.GetByIdAsync(cafeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cafeteria?)null);

        var sut = CreateSut(users, cafeterias);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.AddAsync(userId, cafeId));
    }

    [Fact]
    public async Task RemoveAsync_IsIdempotent_WhenMissing()
    {
        var userId = Guid.NewGuid();
        var cafeId = Guid.NewGuid();
        var favorites = new Mock<IConsumerFavoriteRepository>();
        favorites.Setup(f => f.GetAsync(userId, cafeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsumerFavorite?)null);

        var sut = CreateSut(favorites: favorites);
        await sut.RemoveAsync(userId, cafeId);
    }
}
