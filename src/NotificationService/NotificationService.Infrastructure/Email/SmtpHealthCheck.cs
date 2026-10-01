using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Email;

public sealed class SmtpHealthCheck(IOptions<SmtpOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var smtp = options.Value;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port, timeout.Token);
            return HealthCheckResult.Healthy($"SMTP {smtp.Host}:{smtp.Port} acepta conexiones.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus,
                $"SMTP {smtp.Host}:{smtp.Port} no disponible: las notificaciones quedan en cola hasta que vuelva.", ex);
        }
    }
}
