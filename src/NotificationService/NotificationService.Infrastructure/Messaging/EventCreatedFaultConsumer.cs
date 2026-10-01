using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Notifications.RecordNotificationFailure;
using Shared.Contracts;

namespace NotificationService.Infrastructure.Messaging;

public sealed class EventCreatedFaultConsumer(ISender sender, ILogger<EventCreatedFaultConsumer> logger)
    : IConsumer<Fault<EventCreated>>
{
    public async Task Consume(ConsumeContext<Fault<EventCreated>> context)
    {
        using var scope = MessageLogScope.Begin(logger, context.Message.Message);
        var reason = context.Message.Exceptions.FirstOrDefault()?.Message ?? "Razón desconocida";
        await sender.Send(
            new RecordNotificationFailureCommand(context.Message.Message, reason),
            context.CancellationToken);
    }
}
