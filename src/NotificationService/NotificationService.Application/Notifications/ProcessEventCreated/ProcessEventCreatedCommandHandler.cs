using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Abstractions;
using NotificationService.Domain;

namespace NotificationService.Application.Notifications.ProcessEventCreated;

public sealed class ProcessEventCreatedCommandHandler(
    INotificationLogRepository repository,
    IEmailSender emailSender,
    ILogger<ProcessEventCreatedCommandHandler> logger) : IRequestHandler<ProcessEventCreatedCommand>
{
    public async Task Handle(ProcessEventCreatedCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;
        var log = await repository.FindByMessageIdAsync(message.MessageId, cancellationToken);

        if (log is null)
        {
            log = NotificationLog.StartProcessing(
                message.MessageId, message.EventId, message.Name, message.OccurredAt,
                message.CorrelationId, PayloadHasher.Hash(message));

            // Se reclama el mensaje antes de enviar para no duplicar correos.
            if (!await repository.TryClaimAsync(log, cancellationToken))
            {
                logger.LogInformation(
                    "MessageId {MessageId} reclamado por otra entrega concurrente; se ignora. CorrelationId={CorrelationId}",
                    message.MessageId, message.CorrelationId);
                return;
            }
        }
        else if (log.IsSent)
        {
            logger.LogInformation(
                "MessageId {MessageId} ya fue notificado, se ignora (idempotencia). CorrelationId={CorrelationId}",
                message.MessageId, message.CorrelationId);
            return;
        }
        else
        {
            // Reintento o redrive desde la DLQ.
            log.RegisterRetry();
            await repository.SaveChangesAsync(cancellationToken);
        }

        await emailSender.SendEventCreatedNotificationAsync(
            message.EventId, message.Name, message.OccurredAt, cancellationToken);

        log.MarkSent();
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Notificación enviada para evento {EventId} (intento {Attempt}). MessageId={MessageId} CorrelationId={CorrelationId}",
            message.EventId, log.Attempts, message.MessageId, message.CorrelationId);
    }
}
