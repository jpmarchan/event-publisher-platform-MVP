using System.Diagnostics;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NotificationService.Api.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var endpoint = configuration["Otel:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            return services;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .SetSampler(new ParentBasedSampler(new DropBackgroundRootSpansSampler()))
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddSource("MassTransit")
                .AddOtlpExporter(options => options.Endpoint = new Uri(endpoint)));

        return services;
    }

    // Descarta spans raíz del polling en segundo plano (outbox, SQS).
    private sealed class DropBackgroundRootSpansSampler : Sampler
    {
        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters) =>
            new(samplingParameters.Kind == ActivityKind.Client ? SamplingDecision.Drop : SamplingDecision.RecordAndSample);
    }
}
