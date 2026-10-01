using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Notifications.ProcessEventCreated;
using Shared.Contracts;

namespace NotificationService.Infrastructure.Messaging;

public sealed class EventCreatedConsumer(ISender sender, ILogger<EventCreatedConsumer> logger) : IConsumer<EventCreated>
{
    public async Task Consume(ConsumeContext<EventCreated> context)
    {
        using var scope = MessageLogScope.Begin(logger, context.Message);
        await sender.Send(new ProcessEventCreatedCommand(context.Message), context.CancellationToken);
    }
}
