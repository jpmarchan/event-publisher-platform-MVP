using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventService.Infrastructure.Caching;

public sealed class RedisHealthCheck([FromKeyedServices(DependencyInjection.RawRedisCacheKey)] IDistributedCache redis)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            await redis.GetAsync("health:probe", timeout.Token);
            return HealthCheckResult.Healthy("Redis responde.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus,
                "Redis no disponible: el listado de eventos se sirve directo desde Postgres.", ex);
        }
    }
}
