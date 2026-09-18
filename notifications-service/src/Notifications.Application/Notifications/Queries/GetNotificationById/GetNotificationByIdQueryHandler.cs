using MediatR;
using Notifications.Application.Common.Exceptions;
using Notifications.Application.Common.Interfaces;
using Notifications.Application.Notifications.Dtos;

namespace Notifications.Application.Notifications.Queries.GetNotificationById;

public sealed class GetNotificationByIdQueryHandler(INotificationRepository repository)
    : IRequestHandler<GetNotificationByIdQuery, NotificationDto>
{
    public async Task<NotificationDto> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Notifications.Notification), request.Id);

        return NotificationDto.FromDomain(notification);
    }
}
