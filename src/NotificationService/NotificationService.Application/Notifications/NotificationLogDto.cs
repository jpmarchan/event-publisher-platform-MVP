using NotificationService.Domain;

namespace NotificationService.Application.Notifications;

public sealed record NotificationLogDto(
    Guid Id,
    Guid MessageId,
    Guid EventId,
    string EventName,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    string Status,
    string? FailureReason,
    int Attempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static NotificationLogDto FromDomain(NotificationLog log) => new(
        log.Id, log.MessageId, log.EventId, log.EventName, log.OccurredAt,
        log.CorrelationId, log.Status.ToString(), log.FailureReason, log.Attempts, log.CreatedAt, log.UpdatedAt);
}
