using EventService.Application.Abstractions;
using EventService.Application.Idempotency;
using EventService.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace EventService.Application.Events.CreateEvent;

public sealed class CreateEventCommandHandler(
    IEventRepository repository,
    IIdempotencyStore idempotencyStore,
    IEventPublisher publisher,
    IUnitOfWork unitOfWork,
    IEventsCache cache,
    ILogger<CreateEventCommandHandler> logger) : IRequestHandler<CreateEventCommand, CreateEventResult>
{
    public async Task<CreateEventResult> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var idempotencyKey = request.IdempotencyKey;
        var requestHash = idempotencyKey is null ? null : CreateEventRequestHasher.Hash(request);

        if (idempotencyKey is not null)
        {
            var replay = await TryReplayAsync(idempotencyKey, requestHash!, cancellationToken);
            if (replay is not null)
                return replay;
        }

        var zones = request.Zones.Select(z => Zone.Create(z.Name, z.Price, z.Capacity));
        var @event = Event.CreatePublished(request.Name, request.Date, request.Venue, zones);

        repository.Add(@event);
        if (idempotencyKey is not null)
            idempotencyStore.Add(IdempotencyRecord.Create(idempotencyKey, requestHash!, @event.Id));

        // Va al outbox: se confirma en la misma transacción que el evento.
        await publisher.PublishEventCreatedAsync(new EventCreated
        {
            MessageId = Guid.NewGuid(),
            EventId = @event.Id,
            Name = @event.Name,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = request.CorrelationId
        }, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateIdempotencyKeyException) when (idempotencyKey is not null)
        {
            logger.LogInformation(
                "Idempotency-Key {IdempotencyKey} confirmada por una request concurrente; se devuelve su resultado.",
                idempotencyKey);
            return await TryReplayAsync(idempotencyKey, requestHash!, cancellationToken)
                ?? throw new InvalidOperationException($"No se encontró el registro de la Idempotency-Key '{idempotencyKey}'.");
        }

        await cache.InvalidateAllAsync(cancellationToken);

        logger.LogInformation(
            "Evento {EventId} creado; EventCreated registrado en el outbox. CorrelationId={CorrelationId}",
            @event.Id, request.CorrelationId);

        return new CreateEventResult(EventDto.FromDomain(@event), Replayed: false);
    }

    private async Task<CreateEventResult?> TryReplayAsync(string key, string requestHash, CancellationToken cancellationToken)
    {
        var record = await idempotencyStore.FindAsync(key, cancellationToken);
        if (record is null)
            return null;

        if (record.RequestHash != requestHash)
            throw new IdempotencyKeyReusedException(key);

        var existing = await repository.GetByIdAsync(record.EventId, cancellationToken)
            ?? throw new InvalidOperationException($"La Idempotency-Key '{key}' apunta a un evento inexistente.");

        return new CreateEventResult(EventDto.FromDomain(existing), Replayed: true);
    }
}
