using EventService.Application.Events;

namespace EventService.Application.Abstractions;

public interface IEventsCache
{
    Task<IReadOnlyList<EventDto>> GetOrLoadAllAsync(
        Func<CancellationToken, Task<IReadOnlyList<EventDto>>> load,
        CancellationToken cancellationToken);

    Task InvalidateAllAsync(CancellationToken cancellationToken);
}
