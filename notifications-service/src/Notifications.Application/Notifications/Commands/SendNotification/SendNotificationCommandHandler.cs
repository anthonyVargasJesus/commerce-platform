using MediatR;
using Notifications.Application.Common.Interfaces;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Commands.SendNotification;

public sealed class SendNotificationCommandHandler(
    INotificationRepository repository,
    INotificationSender sender,
    IUnitOfWork unitOfWork) : IRequestHandler<SendNotificationCommand>
{
    public async Task Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var message = NotificationMessages.For(request.Type, request.CustomerName, request.OrderId, request.TotalAmount);

        var notification = Notification.Create(request.OrderId, request.CustomerId, request.CustomerEmail, request.Type, message);

        await sender.SendAsync(notification, cancellationToken);

        repository.Add(notification);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
