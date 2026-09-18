namespace Notifications.Domain.Notifications;

public enum NotificationType
{
    OrderCreated = 0,
    OrderConfirmed = 1,
    OrderShipped = 2,
    OrderDelivered = 3,
    OrderCancelled = 4,
    LowStock = 5,
}
