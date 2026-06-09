namespace Fmc.Domain.Entities;

public class CafeteriaPhoto
{
    public Guid Id { get; set; }
    public Guid CafeteriaId { get; set; }
    public string StorageKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public Guid AuthorUserId { get; set; }
    public string AuthorRole { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public Cafeteria? Cafeteria { get; set; }
}
