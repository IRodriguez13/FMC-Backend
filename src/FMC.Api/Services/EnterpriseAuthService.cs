using Fmc.Api.Contracts;
using Fmc.Api.Entities;

namespace Fmc.Api.Services;

public interface IEnterpriseAuthService
{
    Task<AuthTokenResponse> RegisterAsync(EnterpriseRegisterRequest request, CancellationToken ct = default);
    Task<AuthTokenResponse> LoginAsync(EnterpriseLoginRequest request, CancellationToken ct = default);
}

public class EnterpriseAuthService(
    AppDbContext db,
    IPasswordHasher passwords,
    IJwtTokenService jwt) : IEnterpriseAuthService
{
    public async Task<AuthTokenResponse> RegisterAsync(EnterpriseRegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var dupEnterprise = await db.EnterpriseUsers.AnyAsync(x => x.Email == email, ct);
        var dupConsumer = await db.ConsumerUsers.AnyAsync(x => x.Email == email, ct);
        if (dupEnterprise || dupConsumer)
            throw new InvalidOperationException("El correo ya está registrado.");

        var cafeteriaId = Guid.NewGuid();
        var enterpriseId = Guid.NewGuid();

        var cafeteriaName = string.IsNullOrWhiteSpace(request.CafeteriaName)
            ? "(Registro pendiente)"
            : request.CafeteriaName.Trim();

        var lat = request.Latitude ?? 0;
        var lng = request.Longitude ?? 0;

        var listingActive = ShouldActivateListing(cafeteriaName, lat, lng);

        var cafeteria = new Cafeteria
        {
            Id = cafeteriaId,
            Name = cafeteriaName,
            Description = string.IsNullOrWhiteSpace(request.CafeteriaDescription) ? null : request.CafeteriaDescription.Trim(),
            Address = string.IsNullOrWhiteSpace(request.CafeteriaAddress) ? null : request.CafeteriaAddress.Trim(),
            Latitude = lat,
            Longitude = lng,
            ListingActive = listingActive,
            DiscountPercent = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var enterprise = new EnterpriseUser
        {
            Id = enterpriseId,
            Email = email,
            PasswordHash = passwords.Hash(request.Password),
            CafeteriaId = cafeteriaId,
            SubscriptionTier = EnterpriseSubscriptionTier.Standard,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Cafeterias.Add(cafeteria);
        db.EnterpriseUsers.Add(enterprise);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var token = jwt.CreateEnterpriseToken(
            enterprise.Id,
            enterprise.Email,
            cafeteria.Id,
            enterprise.SubscriptionTier);

        return new AuthTokenResponse(token, AuthRoles.Enterprise, null, cafeteria.Id, enterprise.SubscriptionTier);
    }

    private static bool ShouldActivateListing(string cafeteriaName, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(cafeteriaName) || cafeteriaName == "(Registro pendiente)")
            return false;
        if (latitude == 0 && longitude == 0)
            return false;
        return true;
    }

    public async Task<AuthTokenResponse> LoginAsync(EnterpriseLoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var enterprise = await db.EnterpriseUsers.Include(e => e.Cafeteria).FirstOrDefaultAsync(x => x.Email == email, ct);
        if (enterprise is null || !passwords.Verify(request.Password, enterprise.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        var token = jwt.CreateEnterpriseToken(
            enterprise.Id,
            enterprise.Email,
            enterprise.CafeteriaId,
            enterprise.SubscriptionTier);

        return new AuthTokenResponse(token, AuthRoles.Enterprise, null, enterprise.CafeteriaId, enterprise.SubscriptionTier);
    }
}
