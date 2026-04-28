using Fmc.Api.Contracts;
using Fmc.Api.Entities;

namespace Fmc.Api.Services;

public interface IEnterpriseCafeteriaService
{
    Task<EnterpriseCafeteriaDto> GetMineAsync(Guid enterpriseUserId, CancellationToken ct = default);

    Task<EnterpriseCafeteriaDto> UpdateAsync(Guid enterpriseUserId, EnterpriseCafeteriaUpdateRequest request, CancellationToken ct = default);

    /// <summary>Simula cambio de plan Enterprise (Premium vs Standard); en producción vendría del cobro.</summary>
    Task<(Guid enterpriseUserId, string email, Guid cafeteriaId, EnterpriseSubscriptionTier tier)> SetEnterpriseSubscriptionTierAsync(
        Guid enterpriseUserId,
        EnterpriseSubscriptionTier tier,
        CancellationToken ct = default);
}

public class EnterpriseCafeteriaService(IEnterpriseUserRepository enterpriseUsers, ICafeteriaRepository cafeterias)
    : IEnterpriseCafeteriaService
{
    public async Task<EnterpriseCafeteriaDto> GetMineAsync(Guid enterpriseUserId, CancellationToken ct = default)
    {
        var eu = await enterpriseUsers.GetByIdAsync(enterpriseUserId, ct)
            ?? throw new KeyNotFoundException("Cuenta enterprise no encontrada.");
        return Map(eu);
    }

    public async Task<EnterpriseCafeteriaDto> UpdateAsync(Guid enterpriseUserId, EnterpriseCafeteriaUpdateRequest request, CancellationToken ct = default)
    {
        var eu = await enterpriseUsers.GetByIdAsync(enterpriseUserId, ct)
            ?? throw new KeyNotFoundException("Cuenta enterprise no encontrada.");

        var cafe = await cafeterias.GetTrackedByIdAsync(eu.CafeteriaId, ct)
            ?? throw new KeyNotFoundException("Cafetería no encontrada.");

        cafe.Name = request.Name.Trim();
        cafe.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        cafe.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        cafe.Latitude = request.Latitude;
        cafe.Longitude = request.Longitude;
        cafe.DiscountPercent = Math.Clamp(request.DiscountPercent, 0, 100);
        cafe.ListingActive = ShouldListingBeActive(cafe.Name, request.Latitude, request.Longitude);
        cafe.UpdatedAt = DateTimeOffset.UtcNow;

        await cafeterias.SaveChangesAsync(ct);
        eu = await enterpriseUsers.GetByIdAsync(enterpriseUserId, ct)
             ?? throw new KeyNotFoundException("Cuenta enterprise no encontrada.");
        return Map(eu);
    }

    public async Task<(Guid enterpriseUserId, string email, Guid cafeteriaId, EnterpriseSubscriptionTier tier)>
        SetEnterpriseSubscriptionTierAsync(Guid enterpriseUserId, EnterpriseSubscriptionTier tier, CancellationToken ct = default)
    {
        var eu = await enterpriseUsers.GetTrackedByIdAsync(enterpriseUserId, ct)
            ?? throw new KeyNotFoundException("Cuenta enterprise no encontrada.");

        eu.SubscriptionTier = tier;
        await enterpriseUsers.SaveChangesAsync(ct);

        return (eu.Id, eu.Email, eu.CafeteriaId, eu.SubscriptionTier);
    }

    private static bool ShouldListingBeActive(string name, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "(Registro pendiente)")
            return false;
        return latitude != 0 || longitude != 0;
    }

    private static EnterpriseCafeteriaDto Map(EnterpriseUser eu)
    {
        var c = eu.Cafeteria;
        return new EnterpriseCafeteriaDto(
            c.Id,
            c.Name,
            c.Description,
            c.Address,
            c.Latitude,
            c.Longitude,
            eu.SubscriptionTier,
            c.ListingActive,
            c.DiscountPercent);
    }
}
