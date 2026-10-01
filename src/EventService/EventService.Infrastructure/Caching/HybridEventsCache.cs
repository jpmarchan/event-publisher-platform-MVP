using EventService.Application.Abstractions;
using EventService.Application.Events;
using Microsoft.Extensions.Caching.Hybrid;

namespace EventService.Infrastructure.Caching;

public sealed class HybridEventsCache(HybridCache cache) : IEventsCache
{
    private const string CacheKey = "events:all";

    public async Task<IReadOnlyList<EventDto>> GetOrLoadAllAsync(
        Func<CancellationToken, Task<IReadOnlyList<EventDto>>> load,
        CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            CacheKey,
            load,
            static async (loader, ct) => await loader(ct),
            cancellationToken: cancellationToken);

    public Task InvalidateAllAsync(CancellationToken cancellationToken) =>
        cache.RemoveAsync(CacheKey, cancellationToken).AsTask();
}
