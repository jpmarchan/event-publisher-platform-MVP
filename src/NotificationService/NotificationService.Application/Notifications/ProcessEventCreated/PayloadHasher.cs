using System.Security.Cryptography;
using System.Text;
using Shared.Contracts;

namespace NotificationService.Application.Notifications.ProcessEventCreated;

public static class PayloadHasher
{
    public static string Hash(EventCreated message)
    {
        var canonical = $"{message.MessageId}|{message.EventId}|{message.Name}|{message.OccurredAt:O}|{message.CorrelationId}|{message.Version}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }
}
