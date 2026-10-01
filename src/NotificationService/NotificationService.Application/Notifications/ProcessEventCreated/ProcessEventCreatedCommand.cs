using MediatR;
using Shared.Contracts;

namespace NotificationService.Application.Notifications.ProcessEventCreated;

public sealed record ProcessEventCreatedCommand(EventCreated Message) : IRequest;
