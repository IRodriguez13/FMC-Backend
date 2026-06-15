using Fmc.Application.Contracts;
using Fmc.Application.Interfaces;
using Fmc.Domain.Entities;

namespace Fmc.Application.Services;

public interface IConsumerFavoriteService
{
    Task<ConsumerFavoritesResponse> ListAsync(Guid consumerUserId, CancellationToken ct = default);
    Task<ConsumerFavoriteIdsResponse> ListIdsAsync(Guid consumerUserId, CancellationToken ct = default);
    Task<ConsumerFavoriteIdsResponse> SyncAsync(Guid consumerUserId, IReadOnlyList<Guid> cafeteriaIds, CancellationToken ct = default);
    Task AddAsync(Guid consumerUserId, Guid cafeteriaId, CancellationToken ct = default);
    Task RemoveAsync(Guid consumerUserId, Guid cafeteriaId, CancellationToken ct = default);
}

public class ConsumerFavoriteService(
    IConsumerUserRepository users,
    ICafeteriaRepository cafeterias,
    IConsumerFavoriteRepository favorites,
    ICafeteriaPhotoRepository photos,
    ICafeteriaReviewRepository reviews,
    IEnterpriseUserRepository enterpriseUsers,
    IFileStorageService storage) : IConsumerFavoriteService
{
    public async Task<ConsumerFavoritesResponse> ListAsync(Guid consumerUserId, CancellationToken ct = default)
    {
        _ = await users.GetByIdAsync(consumerUserId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        var list = await favorites.ListByConsumerIdAsync(consumerUserId, ct);
        if (list.Count == 0)
            return new ConsumerFavoritesResponse([]);

        var cafeteriaIds = list.Select(f => f.CafeteriaId).ToList();
        var coverKeys = await photos.GetLatestStorageKeyByCafeteriaIdsAsync(cafeteriaIds, ct);
        var reviewSummaries = await reviews.GetSummariesByCafeteriaIdsAsync(cafeteriaIds, ct);

        var items = new List<ConsumerFavoriteItemDto>();
        foreach (var fav in list)
        {
            var cafe = await cafeterias.GetByIdAsync(fav.CafeteriaId, ct);
            if (cafe is null) continue;

            var eu = await enterpriseUsers.GetByCafeteriaIdAsync(fav.CafeteriaId, ct);
            coverKeys.TryGetValue(fav.CafeteriaId, out var coverKey);
            double? avg = null;
            int? count = null;
            if (reviewSummaries.TryGetValue(fav.CafeteriaId, out var summary) && summary.TotalCount > 0)
            {
                avg = summary.AverageRating;
                count = summary.TotalCount;
            }

            items.Add(new ConsumerFavoriteItemDto(
                fav.CafeteriaId,
                cafe.Name,
                cafe.Address,
                avg,
                count,
                coverKey is not null ? storage.GetPublicUrl(coverKey) : null,
                eu?.SubscriptionTier ?? EnterpriseSubscriptionTier.Standard,
                cafe.DiscountPercent > 0 ? cafe.DiscountPercent : null,
                fav.CreatedAt));
        }

        return new ConsumerFavoritesResponse(items);
    }

    public async Task<ConsumerFavoriteIdsResponse> ListIdsAsync(Guid consumerUserId, CancellationToken ct = default)
    {
        _ = await users.GetByIdAsync(consumerUserId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        var list = await favorites.ListByConsumerIdAsync(consumerUserId, ct);
        return new ConsumerFavoriteIdsResponse(list.Select(f => f.CafeteriaId).ToList());
    }

    public async Task<ConsumerFavoriteIdsResponse> SyncAsync(
        Guid consumerUserId,
        IReadOnlyList<Guid> cafeteriaIds,
        CancellationToken ct = default)
    {
        _ = await users.GetByIdAsync(consumerUserId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        var existing = await favorites.ListByConsumerIdAsync(consumerUserId, ct);
        var existingIds = existing.Select(f => f.CafeteriaId).ToHashSet();

        foreach (var cafeteriaId in cafeteriaIds.Distinct())
        {
            if (existingIds.Contains(cafeteriaId)) continue;
            if (await cafeterias.GetByIdAsync(cafeteriaId, ct) is null) continue;

            await favorites.AddAsync(new ConsumerFavorite
            {
                Id = Guid.NewGuid(),
                ConsumerUserId = consumerUserId,
                CafeteriaId = cafeteriaId,
                CreatedAt = DateTimeOffset.UtcNow,
            }, ct);
        }

        return await ListIdsAsync(consumerUserId, ct);
    }

    public async Task AddAsync(Guid consumerUserId, Guid cafeteriaId, CancellationToken ct = default)
    {
        _ = await users.GetByIdAsync(consumerUserId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");
        _ = await cafeterias.GetByIdAsync(cafeteriaId, ct)
            ?? throw new KeyNotFoundException("Cafetería no encontrada.");

        if (await favorites.GetAsync(consumerUserId, cafeteriaId, ct) is not null)
            return;

        await favorites.AddAsync(new ConsumerFavorite
        {
            Id = Guid.NewGuid(),
            ConsumerUserId = consumerUserId,
            CafeteriaId = cafeteriaId,
            CreatedAt = DateTimeOffset.UtcNow,
        }, ct);
    }

    public async Task RemoveAsync(Guid consumerUserId, Guid cafeteriaId, CancellationToken ct = default)
    {
        var fav = await favorites.GetAsync(consumerUserId, cafeteriaId, ct);
        if (fav is null) return;
        await favorites.DeleteAsync(fav, ct);
    }
}
