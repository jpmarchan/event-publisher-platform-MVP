using Amazon.SimpleNotificationService;
using Amazon.SQS;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MimeKit;
using NotificationService.Application.Abstractions;
using NotificationService.Infrastructure.Email;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public const string ReadyTag = "ready";

    private const int ConcurrentMessageLimit = 8;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationServiceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationServiceDb"), npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IEmailSender, MailKitEmailSender>();

        services.AddHealthChecks()
            .AddDbContextCheck<NotificationServiceDbContext>("postgres", tags: [ReadyTag])
            .AddCheck<SmtpHealthCheck>("smtp", failureStatus: HealthStatus.Degraded, tags: [ReadyTag]);

        services.AddMassTransit(x =>
        {
            x.AddConsumer<EventCreatedConsumer>();
            x.AddConsumer<EventCreatedFaultConsumer>();
            x.AddConfigureEndpointsCallback((_, endpoint) => endpoint.ConcurrentMessageLimit = ConcurrentMessageLimit);

            x.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host(GetRequired(configuration, "Aws:Region"), host =>
                {
                    host.AccessKey(GetRequired(configuration, "Aws:AccessKey"));
                    host.SecretKey(GetRequired(configuration, "Aws:SecretKey"));
                    host.Config(new AmazonSQSConfig { ServiceURL = configuration["Aws:ServiceUrl"] });
                    host.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = configuration["Aws:ServiceUrl"] });
                });

                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(retryLimit: 4, minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(15), intervalDelta: TimeSpan.FromSeconds(2));
                    // Un email mal formado no se arregla reintentando.
                    r.Ignore<ParseException>();
                });

                // El umbral se mide sobre intentos (incluye reintentos) y la ventana debe superar la duración de los reintentos.
                cfg.UseKillSwitch(options => options
                    .SetActivationThreshold(5)
                    .SetTripThreshold(0.15)
                    .SetTrackingPeriod(m: 3)
                    .SetRestartTimeout(m: 1));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static string GetRequired(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Falta la configuración obligatoria '{key}'.");
}
