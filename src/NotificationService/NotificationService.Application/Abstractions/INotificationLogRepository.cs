using NotificationService.Domain;

namespace NotificationService.Application.Abstractions;

public interface INotificationLogRepository
{
    Task<NotificationLog?> FindByMessageIdAsync(Guid messageId, CancellationToken cancellationToken);

    Task<bool> TryClaimAsync(NotificationLog log, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationLog>> GetAllAsync(CancellationToken cancellationToken);
}
