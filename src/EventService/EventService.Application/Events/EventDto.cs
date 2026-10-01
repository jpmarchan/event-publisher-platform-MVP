using EventService.Domain;

namespace EventService.Application.Events;

public sealed record ZoneDto(Guid Id, string Name, decimal Price, int Capacity);

public sealed record EventDto(
    Guid Id,
    string Name,
    DateTimeOffset Date,
    string Venue,
    string Status,
    IReadOnlyCollection<ZoneDto> Zones)
{
    public static EventDto FromDomain(Event @event) => new(
        @event.Id,
        @event.Name,
        @event.Date,
        @event.Venue,
        @event.Status.ToString(),
        @event.Zones.Select(z => new ZoneDto(z.Id, z.Name, z.Price, z.Capacity)).ToList());
}
