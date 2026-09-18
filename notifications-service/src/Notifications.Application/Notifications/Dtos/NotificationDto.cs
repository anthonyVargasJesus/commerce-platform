using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Dtos;

public sealed record NotificationDto(Guid Id, Guid OrderId, Guid CustomerId, NotificationType Type, string Message, DateTimeOffset CreatedAt)
{
    public static NotificationDto FromDomain(Notification notification) =>
        new(notification.Id, notification.OrderId, notification.CustomerId, notification.Type, notification.Message, notification.CreatedAt);
}
