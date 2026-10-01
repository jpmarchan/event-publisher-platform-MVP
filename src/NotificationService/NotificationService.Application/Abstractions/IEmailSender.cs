namespace NotificationService.Application.Abstractions;

public interface IEmailSender
{
    Task SendEventCreatedNotificationAsync(Guid eventId, string eventName, DateTimeOffset occurredAt, CancellationToken cancellationToken);
}
