namespace Shared.Contracts;

public sealed record EventCreated
{
    public required Guid MessageId { get; init; }
    public required Guid EventId { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid CorrelationId { get; init; }
    public int Version { get; init; } = 1;
}
