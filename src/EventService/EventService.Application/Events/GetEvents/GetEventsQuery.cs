using EventService.Application.Events;
using MediatR;

namespace EventService.Application.Events.GetEvents;

public sealed record GetEventsQuery : IRequest<IReadOnlyList<EventDto>>;
