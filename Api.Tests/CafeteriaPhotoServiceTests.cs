using Fmc.Application.Configuration;
using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Constants;
using Fmc.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;

namespace Fmc.Api.Tests;

public class CafeteriaPhotoServiceTests
{
    private static readonly MediaOptions DefaultMediaOptions = new()
    {
        UploadRoot = "uploads-test",
        PublicUrlPath = "/media",
        MaxFileSizeBytes = 1024,
    };

    private static Cafeteria CreateCafeteria(Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test Cafe",
            Latitude = -34.6037,
            Longitude = -58.3816,
            ListingActive = true,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    private static CafeteriaPhotoService CreateSut(
        Mock<ICafeteriaRepository>? cafeterias = null,
        Mock<ICafeteriaPhotoRepository>? photos = null,
        Mock<IFileStorageService>? storage = null,
        MediaOptions? mediaOptions = null)
    {
        cafeterias ??= new Mock<ICafeteriaRepository>();
        photos ??= new Mock<ICafeteriaPhotoRepository>();
        storage ??= new Mock<IFileStorageService>();

        storage.Setup(s => s.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"/media/{key}");
        storage.Setup(s => s.SaveImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("abc123.jpg");

        return new CafeteriaPhotoService(
            cafeterias.Object,
            photos.Object,
            storage.Object,
            Options.Create(mediaOptions ?? DefaultMediaOptions));
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedPhotos_WhenCafeteriaExists()
    {
        var cafe = CreateCafeteria();
        var photo = new CafeteriaPhoto
        {
            Id = Guid.NewGuid(),
            CafeteriaId = cafe.Id,
            StorageKey = "photo.jpg",
            ContentType = "image/jpeg",
            AuthorUserId = Guid.NewGuid(),
            AuthorRole = AuthRoles.Consumer,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var photos = new Mock<ICafeteriaPhotoRepository>();
        photos.Setup(r => r.ListByCafeteriaIdAsync(cafe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CafeteriaPhoto> { photo });

        var sut = CreateSut(cafeterias, photos);

        var result = await sut.ListAsync(cafe.Id);

        Assert.Single(result.Items);
        Assert.Equal("/media/photo.jpg", result.Items[0].Url);
        Assert.Equal(AuthRoles.Consumer, result.Items[0].AuthorRole);
    }

    [Fact]
    public async Task ListAsync_Throws_WhenCafeteriaNotFound()
    {
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cafeteria?)null);

        var sut = CreateSut(cafeterias);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.ListAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UploadAsync_PersistsPhoto_WhenInputValid()
    {
        var cafe = CreateCafeteria();
        var authorId = Guid.NewGuid();
        CafeteriaPhoto? captured = null;

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var photos = new Mock<ICafeteriaPhotoRepository>();
        photos.Setup(r => r.AddAsync(It.IsAny<CafeteriaPhoto>(), It.IsAny<CancellationToken>()))
            .Callback<CafeteriaPhoto, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync((CafeteriaPhoto p, CancellationToken _) => p);

        var sut = CreateSut(cafeterias, photos);
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);

        var dto = await sut.UploadAsync(
            cafe.Id,
            authorId,
            AuthRoles.Enterprise,
            stream,
            "image/jpeg",
            contentLength: 3);

        Assert.NotNull(captured);
        Assert.Equal(cafe.Id, captured!.CafeteriaId);
        Assert.Equal(authorId, captured.AuthorUserId);
        Assert.Equal(AuthRoles.Enterprise, captured.AuthorRole);
        Assert.Equal("/media/abc123.jpg", dto.Url);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UploadAsync_Throws_WhenFileEmpty(long contentLength)
    {
        var cafe = CreateCafeteria();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var sut = CreateSut(cafeterias);
        await using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UploadAsync(cafe.Id, Guid.NewGuid(), AuthRoles.Consumer, stream, "image/jpeg", contentLength));
    }

    [Fact]
    public async Task UploadAsync_Throws_WhenFileTooLarge()
    {
        var cafe = CreateCafeteria();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var sut = CreateSut(cafeterias, mediaOptions: DefaultMediaOptions);
        await using var stream = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UploadAsync(cafe.Id, Guid.NewGuid(), AuthRoles.Consumer, stream, "image/jpeg", contentLength: 2048));
    }

    [Fact]
    public async Task UploadAsync_Throws_WhenContentTypeNotAllowed()
    {
        var cafe = CreateCafeteria();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var sut = CreateSut(cafeterias);
        await using var stream = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UploadAsync(cafe.Id, Guid.NewGuid(), AuthRoles.Consumer, stream, "application/pdf", contentLength: 3));
    }

    [Fact]
    public async Task UploadAsync_Throws_WhenCafeteriaNotFound()
    {
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cafeteria?)null);

        var sut = CreateSut(cafeterias);
        await using var stream = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.UploadAsync(Guid.NewGuid(), Guid.NewGuid(), AuthRoles.Consumer, stream, "image/jpeg", contentLength: 3));
    }
}
