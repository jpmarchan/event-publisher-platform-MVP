using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications.ProcessEventCreated;
using NotificationService.Domain;

namespace NotificationService.Application.Notifications.RecordNotificationFailure;

public sealed class RecordNotificationFailureCommandHandler(
    INotificationLogRepository repository,
    ILogger<RecordNotificationFailureCommandHandler> logger) : IRequestHandler<RecordNotificationFailureCommand>
{
    public async Task Handle(RecordNotificationFailureCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;
        var log = await repository.FindByMessageIdAsync(message.MessageId, cancellationToken);

        if (log is null)
        {
            var failed = NotificationLog.CreateFailed(
                message.MessageId, message.EventId, message.Name, message.OccurredAt,
                message.CorrelationId, PayloadHasher.Hash(message), request.Reason);
            await repository.TryClaimAsync(failed, cancellationToken);
        }
        else if (log.IsSent)
        {
            logger.LogWarning(
                "Fault recibido para MessageId {MessageId}, pero la notificación ya figura enviada; se ignora.",
                message.MessageId);
            return;
        }
        else
        {
            log.MarkFailed(request.Reason);
            await repository.SaveChangesAsync(cancellationToken);
        }

        logger.LogError(
            "Notificación para evento {EventId} agotó reintentos y pasó a DLQ. MessageId={MessageId} Razón={Reason}",
            message.EventId, message.MessageId, request.Reason);
    }
}
