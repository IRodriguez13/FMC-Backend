namespace Fmc.Api.Entities;

/// <summary>Cafetería asociada obligatoriamente a una cuenta Enterprise para poder listarse.</summary>
public class Cafeteria
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Solo si es true la cafetería puede aparecer en descubrimiento (registro completado por Enterprise).</summary>
    public bool ListingActive { get; set; }

    /// <summary>Descuento comercial (0–100). Solo se expone en la API a consumidores Premium.</summary>
    public int DiscountPercent { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public EnterpriseUser? EnterpriseUser { get; set; }
}
