namespace Fmc.Application.Interfaces;

public interface IDiscoveryReadCache
{
    Task<T> GetOrCreateAsync<T>(
        string keySuffix,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default);

    void InvalidateDiscovery();
}
