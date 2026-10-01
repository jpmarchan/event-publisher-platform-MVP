using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Abstractions;
using NotificationService.Domain;
using Npgsql;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationLogRepository(NotificationServiceDbContext dbContext) : INotificationLogRepository
{
    public Task<NotificationLog?> FindByMessageIdAsync(Guid messageId, CancellationToken cancellationToken) =>
        dbContext.NotificationLogs.FirstOrDefaultAsync(n => n.MessageId == messageId, cancellationToken);

    public async Task<bool> TryClaimAsync(NotificationLog log, CancellationToken cancellationToken)
    {
        dbContext.NotificationLogs.Add(log);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // La restricción única decide entre entregas concurrentes.
            dbContext.Entry(log).State = EntityState.Detached;
            return false;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<NotificationLog>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.NotificationLogs
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
