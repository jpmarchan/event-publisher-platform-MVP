using MediatR;
using NotificationService.Application.Notifications;

namespace NotificationService.Application.Notifications.GetNotifications;

public sealed record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationLogDto>>;
