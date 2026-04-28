using Fmc.Api.Contracts;

namespace Fmc.Api.Services;

public interface IConsumerProfileService
{
    Task<ConsumerProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<ConsumerProfileDto> SetTierAsync(Guid userId, ConsumerTier newTier, CancellationToken ct = default);
}

public class ConsumerProfileService(IConsumerUserRepository users) : IConsumerProfileService
{
    public async Task<ConsumerProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");
        return new ConsumerProfileDto(user.Id, user.Email, user.Tier);
    }

    public async Task<ConsumerProfileDto> SetTierAsync(Guid userId, ConsumerTier newTier, CancellationToken ct = default)
    {
        var user = await users.GetTrackedByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        user.Tier = newTier;
        await users.SaveChangesAsync(ct);
        return new ConsumerProfileDto(user.Id, user.Email, user.Tier);
    }
}
