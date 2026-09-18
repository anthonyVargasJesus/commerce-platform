using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Dtos;

public sealed record NotificationDto(Guid Id, Guid? OrderId, Guid? CustomerId, Guid? ProductId, NotificationType Type, string Message, DateTimeOffset CreatedAt)
{
    public static NotificationDto FromDomain(Notification notification) =>
        new(notification.Id, notification.OrderId, notification.CustomerId, notification.ProductId, notification.Type, notification.Message, notification.CreatedAt);
}
