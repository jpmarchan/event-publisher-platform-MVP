namespace EventService.Api.Events;

public sealed record CreateZoneRequest(string Name, decimal Price, int Capacity);

public sealed record CreateEventRequest(
    string Name,
    DateTimeOffset Date,
    string Venue,
    List<CreateZoneRequest> Zones);
