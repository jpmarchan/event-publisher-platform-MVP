namespace NotificationService.Domain;

public sealed class NotificationLog
{
    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid EventId { get; private set; }
    public string EventName { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string PayloadHash { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private NotificationLog(
        Guid messageId, Guid eventId, string eventName, DateTimeOffset occurredAt,
        Guid correlationId, string payloadHash, NotificationStatus status, string? failureReason, int attempts)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        EventId = eventId;
        EventName = eventName;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
        PayloadHash = payloadHash;
        Status = status;
        FailureReason = failureReason;
        Attempts = attempts;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static NotificationLog StartProcessing(
        Guid messageId, Guid eventId, string eventName, DateTimeOffset occurredAt,
        Guid correlationId, string payloadHash) =>
        new(messageId, eventId, eventName, occurredAt, correlationId, payloadHash, NotificationStatus.Processing, null, attempts: 1);

    public static NotificationLog CreateFailed(
        Guid messageId, Guid eventId, string eventName, DateTimeOffset occurredAt,
        Guid correlationId, string payloadHash, string failureReason) =>
        new(messageId, eventId, eventName, occurredAt, correlationId, payloadHash, NotificationStatus.Failed, failureReason, attempts: 0);

    public bool IsSent => Status == NotificationStatus.Sent;

    public void RegisterRetry()
    {
        if (IsSent)
            throw new InvalidOperationException("Una notificación enviada no se reintenta.");

        Attempts++;
        Status = NotificationStatus.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        FailureReason = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        if (IsSent)
            return;

        Status = NotificationStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private NotificationLog()
    {
        EventName = string.Empty;
        PayloadHash = string.Empty;
    }
}
