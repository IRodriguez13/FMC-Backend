namespace Fmc.Domain.Entities;

public class EnterpriseCoupon
{
    public Guid Id { get; set; }
    public Guid CafeteriaId { get; set; }
    public CouponKind Kind { get; set; }
    public int DiscountPercent { get; set; }
    public int FixedAmountArs { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Code { get; set; } = null!;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Cafeteria? Cafeteria { get; set; }
}
