using Fmc.Application.Caching;
using Fmc.Application.Configuration;
using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Fmc.Domain.Constants;
using Fmc.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;

namespace Fmc.Api.Tests;

public class CafeteriaReviewServiceTests
{
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

    private static CafeteriaReviewService CreateSut(
        Mock<ICafeteriaRepository>? cafeterias = null,
        Mock<ICafeteriaReviewRepository>? reviews = null,
        Mock<IFileStorageService>? storage = null)
    {
        cafeterias ??= new Mock<ICafeteriaRepository>();
        reviews ??= new Mock<ICafeteriaReviewRepository>();
        storage ??= new Mock<IFileStorageService>();
        storage.Setup(s => s.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"/media/{key}");

        return new CafeteriaReviewService(
            cafeterias.Object,
            reviews.Object,
            storage.Object,
            new PassthroughDiscoveryReadCache(),
            Options.Create(new MediaOptions { MaxFileSizeBytes = 5_242_880 }));
    }

    [Fact]
    public async Task ListAsync_ReturnsAverageAndCount_WhenReviewsExist()
    {
        var cafe = CreateCafeteria();
        var reviews = new List<CafeteriaReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CafeteriaId = cafe.Id,
                AuthorUserId = Guid.NewGuid(),
                AuthorRole = AuthRoles.Consumer,
                Rating = 4,
                Text = "Bueno",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                CafeteriaId = cafe.Id,
                AuthorUserId = Guid.NewGuid(),
                AuthorRole = AuthRoles.Enterprise,
                Rating = 2,
                Text = "Regular",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
        };

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.ListByCafeteriaIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reviews);

        var sut = CreateSut(cafeterias, reviewRepo);

        var result = await sut.ListAsync(cafe.Id);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(3, result.AverageRating);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListAsync_ReturnsNullAverage_WhenNoReviews()
    {
        var cafe = CreateCafeteria();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.ListByCafeteriaIdAsync(cafe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CafeteriaReview>());

        var sut = CreateSut(cafeterias, reviewRepo);

        var result = await sut.ListAsync(cafe.Id);

        Assert.Empty(result.Items);
        Assert.Null(result.AverageRating);
        Assert.Equal(0, result.TotalCount);
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
    public async Task CreateOrUpdateAsync_CreatesReview_WhenAuthorHasNone()
    {
        var cafe = CreateCafeteria();
        var authorId = Guid.NewGuid();
        CafeteriaReview? captured = null;

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.GetByAuthorAsync(cafe.Id, authorId, AuthRoles.Consumer, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CafeteriaReview?)null);
        reviewRepo.Setup(r => r.AddAsync(It.IsAny<CafeteriaReview>(), It.IsAny<CancellationToken>()))
            .Callback<CafeteriaReview, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync((CafeteriaReview r, CancellationToken _) => r);

        var sut = CreateSut(cafeterias, reviewRepo);

        var dto = await sut.CreateOrUpdateAsync(
            cafe.Id,
            authorId,
            AuthRoles.Consumer,
            new CafeteriaReviewCreateRequest(5, "  Excelente  "));

        Assert.NotNull(captured);
        Assert.Equal(5, captured!.Rating);
        Assert.Equal("Excelente", captured.Text);
        Assert.Equal(5, dto.Rating);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_UpdatesReview_WhenAuthorAlreadyReviewed()
    {
        var cafe = CreateCafeteria();
        var authorId = Guid.NewGuid();
        var existing = new CafeteriaReview
        {
            Id = Guid.NewGuid(),
            CafeteriaId = cafe.Id,
            AuthorUserId = authorId,
            AuthorRole = AuthRoles.Enterprise,
            Rating = 3,
            Text = "Antes",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.GetByAuthorAsync(cafe.Id, authorId, AuthRoles.Enterprise, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = CreateSut(cafeterias, reviewRepo);

        var dto = await sut.CreateOrUpdateAsync(
            cafe.Id,
            authorId,
            AuthRoles.Enterprise,
            new CafeteriaReviewCreateRequest(1, "Actualizado"));

        reviewRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        reviewRepo.Verify(r => r.AddAsync(It.IsAny<CafeteriaReview>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(1, existing.Rating);
        Assert.Equal("Actualizado", existing.Text);
        Assert.Equal(1, dto.Rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateOrUpdateAsync_Throws_WhenRatingOutOfRange(int rating)
    {
        var cafe = CreateCafeteria();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var sut = CreateSut(cafeterias);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateOrUpdateAsync(cafe.Id, Guid.NewGuid(), AuthRoles.Consumer, new CafeteriaReviewCreateRequest(rating, null)));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_Throws_WhenTextTooLong()
    {
        var cafe = CreateCafeteria();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var sut = CreateSut(cafeterias);
        var longText = new string('x', 2001);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateOrUpdateAsync(cafe.Id, Guid.NewGuid(), AuthRoles.Consumer, new CafeteriaReviewCreateRequest(4, longText)));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_Throws_WhenCafeteriaNotFound()
    {
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cafeteria?)null);

        var sut = CreateSut(cafeterias);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.CreateOrUpdateAsync(Guid.NewGuid(), Guid.NewGuid(), AuthRoles.Consumer, new CafeteriaReviewCreateRequest(4, null)));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedReview()
    {
        var cafe = CreateCafeteria();
        var authorId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var existing = new CafeteriaReview
        {
            Id = reviewId,
            CafeteriaId = cafe.Id,
            AuthorUserId = authorId,
            AuthorRole = AuthRoles.Consumer,
            Rating = 3,
            Text = "Antes",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.GetByIdAsync(reviewId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut(cafeterias, reviewRepo);

        var dto = await sut.UpdateAsync(
            cafe.Id, reviewId, authorId, AuthRoles.Consumer, new CafeteriaReviewUpdateRequest(5, "Nuevo"));

        reviewRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(5, dto.Rating);
        Assert.Equal("Nuevo", dto.Text);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenNotOwner()
    {
        var cafe = CreateCafeteria();
        var reviewId = Guid.NewGuid();
        var existing = new CafeteriaReview
        {
            Id = reviewId,
            CafeteriaId = cafe.Id,
            AuthorUserId = Guid.NewGuid(),
            AuthorRole = AuthRoles.Consumer,
            Rating = 3,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.GetByIdAsync(reviewId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut(cafeterias, reviewRepo);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.UpdateAsync(cafe.Id, reviewId, Guid.NewGuid(), AuthRoles.Consumer, new CafeteriaReviewUpdateRequest(1, null)));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnedReview()
    {
        var cafe = CreateCafeteria();
        var authorId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var existing = new CafeteriaReview
        {
            Id = reviewId,
            CafeteriaId = cafe.Id,
            AuthorUserId = authorId,
            AuthorRole = AuthRoles.Enterprise,
            Rating = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.GetByIdAsync(reviewId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut(cafeterias, reviewRepo);

        await sut.DeleteAsync(cafe.Id, reviewId, authorId, AuthRoles.Enterprise);

        reviewRepo.Verify(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenReviewNotFound()
    {
        var cafe = CreateCafeteria();
        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CafeteriaReview?)null);

        var sut = CreateSut(cafeterias, reviewRepo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.DeleteAsync(cafe.Id, Guid.NewGuid(), Guid.NewGuid(), AuthRoles.Consumer));
    }

    [Fact]
    public async Task UploadPhotoAsync_SetsStorageKey_OnOwnedReview()
    {
        var cafe = CreateCafeteria();
        var authorId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var existing = new CafeteriaReview
        {
            Id = reviewId,
            CafeteriaId = cafe.Id,
            AuthorUserId = authorId,
            AuthorRole = AuthRoles.Consumer,
            Rating = 4,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.GetByIdAsync(reviewId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.SaveImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("review.jpg");
        storage.Setup(s => s.GetPublicUrl("review.jpg")).Returns("/media/review.jpg");

        var sut = CreateSut(cafeterias, reviewRepo, storage);
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);

        var dto = await sut.UploadPhotoAsync(
            cafe.Id, reviewId, authorId, AuthRoles.Consumer, stream, "image/jpeg", 3);

        reviewRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("review.jpg", existing.PhotoStorageKey);
        Assert.Equal("/media/review.jpg", dto.PhotoUrl);
    }

    [Fact]
    public async Task DeletePhotoAsync_ClearsStorageKey_OnOwnedReview()
    {
        var cafe = CreateCafeteria();
        var authorId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var existing = new CafeteriaReview
        {
            Id = reviewId,
            CafeteriaId = cafe.Id,
            AuthorUserId = authorId,
            AuthorRole = AuthRoles.Consumer,
            Rating = 4,
            PhotoStorageKey = "old.jpg",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var cafeterias = new Mock<ICafeteriaRepository>();
        cafeterias.Setup(r => r.GetByIdAsync(cafe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cafe);

        var reviewRepo = new Mock<ICafeteriaReviewRepository>();
        reviewRepo.Setup(r => r.GetByIdAsync(reviewId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut(cafeterias, reviewRepo);

        await sut.DeletePhotoAsync(cafe.Id, reviewId, authorId, AuthRoles.Consumer);

        Assert.Null(existing.PhotoStorageKey);
        reviewRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
