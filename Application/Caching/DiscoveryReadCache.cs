using Fmc.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Fmc.Application.Caching;

public sealed class DiscoveryReadCache(IMemoryCache cache) : IDiscoveryReadCache
{
    private long _generation;

    public async Task<T> GetOrCreateAsync<T>(
        string keySuffix,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default)
    {
        var key = $"d:{Volatile.Read(ref _generation)}:{keySuffix}";
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory(ct);
        cache.Set(key, value, ttl);
        return value;
    }

    public void InvalidateDiscovery() =>
        Interlocked.Increment(ref _generation);
}

/// <summary>Evita caché en tests unitarios que mockean repositorios.</summary>
public sealed class PassthroughDiscoveryReadCache : IDiscoveryReadCache
{
    public Task<T> GetOrCreateAsync<T>(
        string keySuffix,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default) =>
        factory(ct);

    public void InvalidateDiscovery() { }
}
