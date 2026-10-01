using Amazon.SimpleNotificationService;
using Amazon.SQS;
using EventService.Application.Abstractions;
using EventService.Infrastructure.Caching;
using EventService.Infrastructure.Messaging;
using EventService.Infrastructure.Persistence;
using EventService.Infrastructure.Persistence.Seed;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using StackExchange.Redis;

namespace EventService.Infrastructure;

public static class DependencyInjection
{
    public const string RawRedisCacheKey = "redis-raw";

    public const string ReadyTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var seedDemoData = bool.TryParse(configuration[DemoDataSeeder.ConfigKey], out var seed) && seed;

        services.AddDbContext<EventServiceDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("EventServiceDb"), npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null));

            if (seedDemoData)
            {
                options.UseAsyncSeeding((context, _, cancellationToken) => DemoDataSeeder.SeedAsync(context, cancellationToken));
                options.UseSeeding((context, _) => DemoDataSeeder.Seed(context));
            }
        });

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        AddResilientCache(services, configuration);
        AddMessaging(services, configuration);

        services.AddHealthChecks()
            .AddDbContextCheck<EventServiceDbContext>("postgres", tags: [ReadyTag])
            // Degraded: sin Redis la Api sigue respondiendo desde Postgres.
            .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded, tags: [ReadyTag]);

        return services;
    }

    private static void AddResilientCache(IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            var redisConfig = ConfigurationOptions.Parse(GetRequired(configuration, "ConnectionStrings:Redis"));
            redisConfig.Password = configuration["Redis:Password"];
            redisConfig.Ssl = bool.TryParse(configuration["Redis:UseTls"], out var useTls) && useTls;
            redisConfig.AbortOnConnectFail = false;
            redisConfig.ConnectTimeout = 2000;
            redisConfig.AsyncTimeout = 1000;
            redisConfig.SyncTimeout = 1000;

            options.ConfigurationOptions = redisConfig;
            options.InstanceName = "eventservice:";
        });

        // El IDistributedCache de Redis queda registrado con clave; el público es el decorator con circuit breaker.
        var redisRegistration = services.Last(d => d.ServiceType == typeof(IDistributedCache) && !d.IsKeyedService);
        services.Remove(redisRegistration);
        services.Add(redisRegistration.ImplementationType is { } implementationType
            ? new ServiceDescriptor(typeof(IDistributedCache), RawRedisCacheKey, implementationType, ServiceLifetime.Singleton)
            : new ServiceDescriptor(typeof(IDistributedCache), RawRedisCacheKey,
                (sp, _) => redisRegistration.ImplementationFactory!(sp), ServiceLifetime.Singleton));

        services.AddResiliencePipeline(ResilientDistributedCache.PipelineName, (builder, context) =>
        {
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RedisCircuitBreaker");
            builder
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 4,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                    OnOpened = args =>
                    {
                        logger.LogWarning(
                            "Circuito de Redis ABIERTO por {BreakDuration}: cache deshabilitado, se lee de Postgres.",
                            args.BreakDuration);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("Circuito de Redis CERRADO: cache restablecido.");
                        return ValueTask.CompletedTask;
                    }
                })
                .AddTimeout(TimeSpan.FromMilliseconds(500));
        });

        services.AddSingleton<IDistributedCache>(sp => new ResilientDistributedCache(
            sp.GetRequiredKeyedService<IDistributedCache>(RawRedisCacheKey),
            sp.GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline(ResilientDistributedCache.PipelineName),
            sp.GetRequiredService<ILogger<ResilientDistributedCache>>()));

        services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(30),
            LocalCacheExpiration = TimeSpan.FromSeconds(10)
        });

        services.AddScoped<IEventsCache, HybridEventsCache>();
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(busConfigurator =>
        {
            // Transactional outbox: los mensajes se guardan con los datos y se entregan en segundo plano.
            busConfigurator.AddEntityFrameworkOutbox<EventServiceDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
            });

            busConfigurator.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host(GetRequired(configuration, "Aws:Region"), host =>
                {
                    host.AccessKey(GetRequired(configuration, "Aws:AccessKey"));
                    host.SecretKey(GetRequired(configuration, "Aws:SecretKey"));
                    host.Config(new AmazonSQSConfig { ServiceURL = configuration["Aws:ServiceUrl"] });
                    host.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = configuration["Aws:ServiceUrl"] });
                });
            });
        });
    }

    private static string GetRequired(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Falta la configuración obligatoria '{key}'.");
}
