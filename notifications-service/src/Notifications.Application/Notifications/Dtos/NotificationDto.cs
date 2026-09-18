using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Dtos;

public sealed record NotificationDto(Guid Id, Guid? OrderId, Guid? CustomerId, Guid? ProductId, string? Recipient, NotificationType Type, string Message, DateTimeOffset CreatedAt)
{
    public static NotificationDto FromDomain(Notification notification) =>
        new(notification.Id, notification.OrderId, notification.CustomerId, notification.ProductId, notification.Recipient, notification.Type, notification.Message, notification.CreatedAt);
}
