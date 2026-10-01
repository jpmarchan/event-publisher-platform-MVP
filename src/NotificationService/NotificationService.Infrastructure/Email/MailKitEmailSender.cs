using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Application.Abstractions;

namespace NotificationService.Infrastructure.Email;

public sealed class MailKitEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendEventCreatedNotificationAsync(
        Guid eventId, string eventName, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        var smtp = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse("promotor@plataforma-eventos.local"));
        message.Subject = $"Evento publicado: {eventName}";
        message.Body = new TextPart("plain")
        {
            Text = $"""
                    Tu evento "{eventName}" (id: {eventId}) fue publicado correctamente.
                    Fecha de publicación: {occurredAt:u}
                    """
        };

        using var client = new SmtpClient { Timeout = (int)TimeSpan.FromSeconds(smtp.TimeoutSeconds).TotalMilliseconds };
        await client.ConnectAsync(smtp.Host, smtp.Port, useSsl: false, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
