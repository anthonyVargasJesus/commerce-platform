using MediatR;
using Notifications.Application.Notifications.Dtos;

namespace Notifications.Application.Notifications.Queries.GetNotificationById;

public sealed record GetNotificationByIdQuery(Guid Id) : IRequest<NotificationDto>;
