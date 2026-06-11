namespace Fmc.Domain.Entities;

public class ConsumerUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? AvatarStorageKey { get; set; }
    public ConsumerTier Tier { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
