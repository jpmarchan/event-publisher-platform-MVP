using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace NotificationService.Infrastructure.Messaging;

internal static class MessageLogScope
{
    public static IDisposable? Begin(ILogger logger, EventCreated message)
    {
        Activity.Current?.SetTag("correlation.id", message.CorrelationId);
        Activity.Current?.SetTag("messaging.message.business_id", message.MessageId);

        return logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = message.CorrelationId,
            ["MessageId"] = message.MessageId
        });
    }
}
