using MediatR;
using Notifications.Application.Common.Interfaces;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Commands.SendStockAlert;

public sealed class SendStockAlertCommandHandler(
    INotificationRepository repository,
    INotificationSender sender,
    IUnitOfWork unitOfWork) : IRequestHandler<SendStockAlertCommand>
{
    public async Task Handle(SendStockAlertCommand request, CancellationToken cancellationToken)
    {
        var message = $"Low stock: {request.Name} ({request.Sku}) has {request.QuantityOnHand} units left (reorder level {request.ReorderLevel}).";

        var notification = Notification.CreateStockAlert(request.ProductId, message);

        await sender.SendAsync(notification, cancellationToken);

        repository.Add(notification);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
