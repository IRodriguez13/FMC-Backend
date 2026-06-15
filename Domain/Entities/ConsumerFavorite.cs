namespace Fmc.Domain.Entities;

public class ConsumerFavorite
{
    public Guid Id { get; set; }
    public Guid ConsumerUserId { get; set; }
    public Guid CafeteriaId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ConsumerUser? ConsumerUser { get; set; }
    public Cafeteria? Cafeteria { get; set; }
}
