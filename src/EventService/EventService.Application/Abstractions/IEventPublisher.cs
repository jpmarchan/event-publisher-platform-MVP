using Shared.Contracts;

namespace EventService.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishEventCreatedAsync(EventCreated message, CancellationToken cancellationToken);
}
