namespace Fmc.Domain.Entities;

public class CafeteriaReview
{
    public Guid Id { get; set; }
    public Guid CafeteriaId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorRole { get; set; } = null!;
    public int Rating { get; set; }
    public string? Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Cafeteria? Cafeteria { get; set; }
}
