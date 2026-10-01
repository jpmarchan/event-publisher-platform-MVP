namespace EventService.Application.Idempotency;

public sealed class IdempotencyRecord
{
    public const int MaxKeyLength = 100;

    public string Key { get; private set; }
    public string RequestHash { get; private set; }
    public Guid EventId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord(string key, string requestHash, Guid eventId)
    {
        Key = key;
        RequestHash = requestHash;
        EventId = eventId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static IdempotencyRecord Create(string key, string requestHash, Guid eventId) =>
        new(key, requestHash, eventId);

    private IdempotencyRecord()
    {
        Key = string.Empty;
        RequestHash = string.Empty;
    }
}
