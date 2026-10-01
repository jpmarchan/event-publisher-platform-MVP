using MediatR;
using Shared.Contracts;

namespace NotificationService.Application.Notifications.RecordNotificationFailure;

public sealed record RecordNotificationFailureCommand(EventCreated Message, string Reason) : IRequest;
