using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace EventService.Infrastructure.Caching;

public sealed class ResilientDistributedCache(
    IDistributedCache inner,
    ResiliencePipeline pipeline,
    ILogger<ResilientDistributedCache> logger) : IDistributedCache
{
    public const string PipelineName = "redis-cache";

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        ExecuteAsync(ct => new ValueTask<byte[]?>(inner.GetAsync(key, ct)), fallback: null, "GET", key, token);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        ExecuteAsync(async ct => { await inner.SetAsync(key, value, options, ct); return true; }, fallback: false, "SET", key, token);

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        ExecuteAsync(async ct => { await inner.RefreshAsync(key, ct); return true; }, fallback: false, "REFRESH", key, token);

    public Task RemoveAsync(string key, CancellationToken token = default) =>
        ExecuteAsync(async ct => { await inner.RemoveAsync(key, ct); return true; }, fallback: false, "REMOVE", key, token);

    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => SetAsync(key, value, options).GetAwaiter().GetResult();
    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();
    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

    private async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, ValueTask<T>> operation, T fallback, string operationName, string key, CancellationToken token)
    {
        try
        {
            return await pipeline.ExecuteAsync(operation, token);
        }
        catch (BrokenCircuitException)
        {
            logger.LogDebug("Cache {Operation} {Key} omitido: circuito de Redis abierto.", operationName, key);
            return fallback;
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Cache {Operation} {Key} falló; se continúa sin cache.", operationName, key);
            return fallback;
        }
    }
}
