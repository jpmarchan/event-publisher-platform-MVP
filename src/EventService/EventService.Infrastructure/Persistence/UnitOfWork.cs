using EventService.Application.Abstractions;
using EventService.Application.Idempotency;
using EventService.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventService.Infrastructure.Persistence;

public sealed class UnitOfWork(EventServiceDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: IdempotencyRecordConfiguration.PrimaryKeyName
        })
        {
            var key = dbContext.ChangeTracker.Entries<IdempotencyRecord>()
                .Select(e => e.Entity.Key)
                .FirstOrDefault() ?? "(desconocida)";

            dbContext.ChangeTracker.Clear();
            throw new DuplicateIdempotencyKeyException(key, ex);
        }
    }
}
