using MediatR;
using Notifications.Application.Common.Interfaces;
using Notifications.Application.Common.Models;
using Notifications.Application.Notifications.Dtos;

namespace Notifications.Application.Notifications.Queries.GetNotificationsList;

public sealed class GetNotificationsListQueryHandler(INotificationRepository repository)
    : IRequestHandler<GetNotificationsListQuery, PaginatedList<NotificationDto>>
{
    public async Task<PaginatedList<NotificationDto>> Handle(GetNotificationsListQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.GetPagedAsync(
            request.CustomerId,
            request.OrderId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return new PaginatedList<NotificationDto>(items.Select(NotificationDto.FromDomain).ToList(), totalCount, request.PageNumber, request.PageSize);
    }
}
