namespace Fmc.Domain.Entities;

/// <summary>Cuenta Enterprise vinculada 1:1 a una cafetería.</summary>
public class EnterpriseUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    /// <summary>Premium mejora la ponderación en listados frente a Enterprise Standard.</summary>
    public EnterpriseSubscriptionTier SubscriptionTier { get; set; }

    public Guid CafeteriaId { get; set; }
    public Cafeteria Cafeteria { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
