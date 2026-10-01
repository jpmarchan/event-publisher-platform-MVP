using MediatR;

namespace EventService.Application.Events.CreateEvent;

public sealed record CreateZoneInput(string Name, decimal Price, int Capacity);

public sealed record CreateEventCommand(
    string Name,
    DateTimeOffset Date,
    string Venue,
    IReadOnlyList<CreateZoneInput> Zones,
    Guid CorrelationId,
    string? IdempotencyKey = null) : IRequest<CreateEventResult>;

public sealed record CreateEventResult(EventDto Event, bool Replayed);
