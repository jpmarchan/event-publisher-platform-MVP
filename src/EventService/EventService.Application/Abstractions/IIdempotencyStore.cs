using EventService.Application.Idempotency;

namespace EventService.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken);
    void Add(IdempotencyRecord record);
}
