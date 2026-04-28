using Fmc.Api.Contracts;
using Fmc.Api.Entities;

namespace Fmc.Api.Services;

public interface IConsumerAuthService
{
    Task<AuthTokenResponse> RegisterAsync(ConsumerRegisterRequest request, CancellationToken ct = default);
    Task<AuthTokenResponse> LoginAsync(ConsumerLoginRequest request, CancellationToken ct = default);
}

public class ConsumerAuthService(
    IConsumerUserRepository users,
    IPasswordHasher passwords,
    IJwtTokenService jwt) : IConsumerAuthService
{
    public async Task<AuthTokenResponse> RegisterAsync(ConsumerRegisterRequest request, CancellationToken ct = default)
    {
        var existing = await users.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException("El correo ya está registrado.");

        var user = new ConsumerUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwords.Hash(request.Password),
            Tier = ConsumerTier.Free,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await users.AddAsync(user, ct);
        var token = jwt.CreateConsumerToken(user.Id, user.Email, user.Tier);
        return new AuthTokenResponse(token, AuthRoles.Consumer, user.Tier, null, null);
    }

    public async Task<AuthTokenResponse> LoginAsync(ConsumerLoginRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        var token = jwt.CreateConsumerToken(user.Id, user.Email, user.Tier);
        return new AuthTokenResponse(token, AuthRoles.Consumer, user.Tier, null, null);
    }
}
