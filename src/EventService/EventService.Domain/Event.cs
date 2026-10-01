namespace EventService.Domain;

public sealed class Event
{
    private readonly List<Zone> _zones = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTimeOffset Date { get; private set; }
    public string Venue { get; private set; }
    public EventStatus Status { get; private set; }
    public IReadOnlyCollection<Zone> Zones => _zones.AsReadOnly();

    private Event(Guid id, string name, DateTimeOffset date, string venue)
    {
        Id = id;
        Name = name;
        Date = date;
        Venue = venue;
        Status = EventStatus.Draft;
    }

    public static Event CreatePublished(string name, DateTimeOffset date, string venue, IEnumerable<Zone> zones)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del evento es obligatorio.");
        if (string.IsNullOrWhiteSpace(venue))
            throw new DomainException("El lugar del evento es obligatorio.");
        if (date <= DateTimeOffset.UtcNow)
            throw new DomainException("La fecha del evento debe ser futura.");

        var zoneList = zones.ToList();
        if (zoneList.Count == 0)
            throw new DomainException("Un evento debe tener al menos una zona.");

        var @event = new Event(Guid.NewGuid(), name.Trim(), date, venue.Trim())
        {
            Status = EventStatus.Published
        };
        @event._zones.AddRange(zoneList);
        return @event;
    }

    private Event()
    {
        Name = string.Empty;
        Venue = string.Empty;
    }
}
