using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;

namespace Fmc.Application.Services;

public interface IConsumerProfileService
{
    Task<ConsumerProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<ConsumerProfileDto> UpdateProfileAsync(Guid userId, ConsumerProfileUpdateRequest request, CancellationToken ct = default);
    Task<ConsumerProfileDto> UploadAvatarAsync(
        Guid userId,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default);
    Task<ConsumerProfileDto> DeleteAvatarAsync(Guid userId, CancellationToken ct = default);
    Task<ConsumerProfileDto> SetTierAsync(Guid userId, ConsumerTier newTier, CancellationToken ct = default);
}

public class ConsumerProfileService(IConsumerUserRepository users, IFileStorageService storage) : IConsumerProfileService
{
    private static readonly HashSet<string> AllowedAvatarTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<ConsumerProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");
        return Map(user);
    }

    public async Task<ConsumerProfileDto> UpdateProfileAsync(
        Guid userId,
        ConsumerProfileUpdateRequest request,
        CancellationToken ct = default)
    {
        var user = await users.GetTrackedByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (request.DisplayName is not null)
        {
            var name = request.DisplayName.Trim();
            if (name.Length == 0)
                throw new ArgumentException("El nombre no puede estar vacío.");
            if (name.Length > 80)
                throw new ArgumentException("El nombre no puede superar 80 caracteres.");
            user.DisplayName = name;
        }

        await users.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<ConsumerProfileDto> UploadAvatarAsync(
        Guid userId,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken ct = default)
    {
        if (contentLength <= 0)
            throw new ArgumentException("Archivo vacío.");
        if (!AllowedAvatarTypes.Contains(contentType))
            throw new ArgumentException("Formato no permitido. Usá JPEG, PNG o WebP.");

        var user = await users.GetTrackedByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        var storageKey = await storage.SaveImageAsync(content, contentType, ct);
        user.AvatarStorageKey = storageKey;
        await users.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<ConsumerProfileDto> DeleteAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetTrackedByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        user.AvatarStorageKey = null;
        await users.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<ConsumerProfileDto> SetTierAsync(Guid userId, ConsumerTier newTier, CancellationToken ct = default)
    {
        var user = await users.GetTrackedByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        user.Tier = newTier;
        await users.SaveChangesAsync(ct);
        return Map(user);
    }

    private ConsumerProfileDto Map(ConsumerUser user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.Email.Split('@')[0]
            : user.DisplayName.Trim();

        string? avatarUrl = string.IsNullOrWhiteSpace(user.AvatarStorageKey)
            ? null
            : storage.GetPublicUrl(user.AvatarStorageKey);

        return new ConsumerProfileDto(user.Id, user.Email, displayName, user.Tier, avatarUrl);
    }
}
