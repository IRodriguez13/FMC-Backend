namespace Fmc.Application.Contracts;

public record CafeteriaPhotoDto(
    Guid Id,
    Guid CafeteriaId,
    string Url,
    string ContentType,
    Guid AuthorUserId,
    string AuthorRole,
    DateTimeOffset CreatedAt);

public record CafeteriaReviewDto(
    Guid Id,
    Guid CafeteriaId,
    int Rating,
    string? Text,
    Guid AuthorUserId,
    string AuthorRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CafeteriaReviewCreateRequest(int Rating, string? Text);

public record CafeteriaPhotosResponse(IReadOnlyList<CafeteriaPhotoDto> Items);

public record CafeteriaReviewsResponse(
    IReadOnlyList<CafeteriaReviewDto> Items,
    double? AverageRating,
    int TotalCount);
