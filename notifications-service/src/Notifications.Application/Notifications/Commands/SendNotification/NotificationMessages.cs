using System.Globalization;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Commands.SendNotification;

internal static class NotificationMessages
{
    public static string For(NotificationType type, Guid orderId, decimal totalAmount)
    {
        var total = totalAmount.ToString("C", CultureInfo.GetCultureInfo("en-US"));

        return type switch
        {
            NotificationType.OrderCreated => $"We received your order {orderId} ({total}).",
            NotificationType.OrderConfirmed => $"Your order {orderId} ({total}) has been confirmed.",
            NotificationType.OrderShipped => $"Your order {orderId} is on its way.",
            NotificationType.OrderDelivered => $"Your order {orderId} was delivered.",
            NotificationType.OrderCancelled => $"Your order {orderId} ({total}) has been cancelled.",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown notification type."),
        };
    }
}
