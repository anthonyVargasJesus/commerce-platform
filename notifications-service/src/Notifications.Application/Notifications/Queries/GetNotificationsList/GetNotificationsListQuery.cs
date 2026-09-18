using MediatR;
using Notifications.Application.Common.Models;
using Notifications.Application.Notifications.Dtos;

namespace Notifications.Application.Notifications.Queries.GetNotificationsList;

public sealed record GetNotificationsListQuery(Guid? CustomerId = null, Guid? OrderId = null, Guid? ProductId = null, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<NotificationDto>>;
