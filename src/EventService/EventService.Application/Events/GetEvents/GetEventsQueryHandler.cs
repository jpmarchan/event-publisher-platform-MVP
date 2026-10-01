using EventService.Application.Abstractions;
using EventService.Application.Events;
using MediatR;

namespace EventService.Application.Events.GetEvents;

public sealed class GetEventsQueryHandler(
    IEventRepository repository,
    IEventsCache cache) : IRequestHandler<GetEventsQuery, IReadOnlyList<EventDto>>
{
    public Task<IReadOnlyList<EventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken) =>
        cache.GetOrLoadAllAsync(async ct =>
        {
            var events = await repository.GetAllAsync(ct);
            return events.Select(EventDto.FromDomain).ToList();
        }, cancellationToken);
}
