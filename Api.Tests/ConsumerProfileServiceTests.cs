using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Entities;
using Moq;

namespace Fmc.Api.Tests;

public class ConsumerProfileServiceTests
{
    private static ConsumerUser CreateUser(
        Guid? id = null,
        string email = "consumidor@seed.fmc",
        string? displayName = null,
        string? avatarStorageKey = null,
        ConsumerTier tier = ConsumerTier.Free)
    {
        return new ConsumerUser
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = "hash",
            DisplayName = displayName,
            AvatarStorageKey = avatarStorageKey,
            Tier = tier,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static ConsumerProfileService CreateSut(
        Mock<IConsumerUserRepository>? users = null,
        Mock<IFileStorageService>? storage = null)
    {
        users ??= new Mock<IConsumerUserRepository>();
        storage ??= new Mock<IFileStorageService>();

        storage.Setup(s => s.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"/media/{key}");

        return new ConsumerProfileService(users.Object, storage.Object);
    }

    [Fact]
    public async Task GetProfileAsync_UsesEmailLocalPart_WhenDisplayNameMissing()
    {
        var user = CreateUser(displayName: null);
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        var result = await sut.GetProfileAsync(user.Id);

        Assert.Equal("consumidor", result.DisplayName);
        Assert.Null(result.AvatarUrl);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsDisplayNameAndAvatar_WhenSet()
    {
        var user = CreateUser(displayName: "  Iván  ", avatarStorageKey: "avatar-abc.jpg");
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        var result = await sut.GetProfileAsync(user.Id);

        Assert.Equal("Iván", result.DisplayName);
        Assert.Equal("/media/avatar-abc.jpg", result.AvatarUrl);
        Assert.Equal(ConsumerTier.Free, result.Tier);
    }

    [Fact]
    public async Task GetProfileAsync_Throws_WhenUserMissing()
    {
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsumerUser?)null);

        var sut = CreateSut(users);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetProfileAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateProfileAsync_PersistsDisplayName()
    {
        var user = CreateUser();
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        var result = await sut.UpdateProfileAsync(user.Id, new ConsumerProfileUpdateRequest("Nuevo nombre"));

        Assert.Equal("Nuevo nombre", result.DisplayName);
        Assert.Equal("Nuevo nombre", user.DisplayName);
        users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_Throws_WhenDisplayNameEmpty()
    {
        var user = CreateUser();
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UpdateProfileAsync(user.Id, new ConsumerProfileUpdateRequest("   ")));
    }

    [Fact]
    public async Task UpdateProfileAsync_Throws_WhenDisplayNameTooLong()
    {
        var user = CreateUser();
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        var longName = new string('a', 81);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UpdateProfileAsync(user.Id, new ConsumerProfileUpdateRequest(longName)));
    }

    [Fact]
    public async Task UploadAvatarAsync_SavesStorageKeyAndReturnsUrl()
    {
        var user = CreateUser();
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.SaveImageAsync(It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync("saved-avatar.jpg");
        storage.Setup(s => s.GetPublicUrl("saved-avatar.jpg")).Returns("/media/saved-avatar.jpg");

        var sut = CreateSut(users, storage);
        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await sut.UploadAvatarAsync(user.Id, stream, "image/jpeg", 3);

        Assert.Equal("saved-avatar.jpg", user.AvatarStorageKey);
        Assert.Equal("/media/saved-avatar.jpg", result.AvatarUrl);
        users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAvatarAsync_Throws_WhenContentTypeInvalid()
    {
        var user = CreateUser();
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        await using var stream = new MemoryStream([1]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UploadAvatarAsync(user.Id, stream, "application/pdf", 1));
    }

    [Fact]
    public async Task UploadAvatarAsync_Throws_WhenEmpty()
    {
        var user = CreateUser();
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        await using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UploadAvatarAsync(user.Id, stream, "image/png", 0));
    }

    [Fact]
    public async Task DeleteAvatarAsync_ClearsStorageKey()
    {
        var user = CreateUser(avatarStorageKey: "avatar-abc.jpg");
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        var result = await sut.DeleteAvatarAsync(user.Id);

        Assert.Null(user.AvatarStorageKey);
        Assert.Null(result.AvatarUrl);
        users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAvatarAsync_IsIdempotent_WhenAlreadyMissing()
    {
        var user = CreateUser(avatarStorageKey: null);
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        var result = await sut.DeleteAvatarAsync(user.Id);

        Assert.Null(result.AvatarUrl);
        users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetTierAsync_UpdatesTier()
    {
        var user = CreateUser(tier: ConsumerTier.Free);
        var users = new Mock<IConsumerUserRepository>();
        users.Setup(r => r.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut(users);
        var result = await sut.SetTierAsync(user.Id, ConsumerTier.Premium);

        Assert.Equal(ConsumerTier.Premium, user.Tier);
        Assert.Equal(ConsumerTier.Premium, result.Tier);
        users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
