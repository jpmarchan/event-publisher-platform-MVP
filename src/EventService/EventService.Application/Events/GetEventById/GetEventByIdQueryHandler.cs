using EventService.Application.Abstractions;
using EventService.Application.Events;
using MediatR;

namespace EventService.Application.Events.GetEventById;

public sealed class GetEventByIdQueryHandler(IEventRepository repository)
    : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        return @event is null ? null : EventDto.FromDomain(@event);
    }
}
