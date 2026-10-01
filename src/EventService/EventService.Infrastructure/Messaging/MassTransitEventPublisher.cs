using EventService.Application.Abstractions;
using MassTransit;
using Shared.Contracts;

namespace EventService.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishEventCreatedAsync(EventCreated message, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(message, cancellationToken);
}
