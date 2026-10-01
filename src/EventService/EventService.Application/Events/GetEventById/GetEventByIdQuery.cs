using EventService.Application.Events;
using MediatR;

namespace EventService.Application.Events.GetEventById;

public sealed record GetEventByIdQuery(Guid Id) : IRequest<EventDto?>;
