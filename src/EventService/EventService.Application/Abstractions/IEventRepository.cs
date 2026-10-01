using EventService.Domain;

namespace EventService.Application.Abstractions;

public interface IEventRepository
{
    void Add(Event @event);
    Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
