using EventService.Application.Abstractions;
using EventService.Application.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence;

public sealed class IdempotencyStore(EventServiceDbContext dbContext) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken) =>
        dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

    public void Add(IdempotencyRecord record) => dbContext.IdempotencyRecords.Add(record);
}
