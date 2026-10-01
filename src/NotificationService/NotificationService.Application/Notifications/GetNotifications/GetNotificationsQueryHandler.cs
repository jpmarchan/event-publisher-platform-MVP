using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Notifications;

namespace NotificationService.Application.Notifications.GetNotifications;

public sealed class GetNotificationsQueryHandler(INotificationLogRepository repository)
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationLogDto>>
{
    public async Task<IReadOnlyList<NotificationLogDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var logs = await repository.GetAllAsync(cancellationToken);
        return logs.Select(NotificationLogDto.FromDomain).ToList();
    }
}
