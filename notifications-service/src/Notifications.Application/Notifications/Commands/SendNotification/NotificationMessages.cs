using System.Globalization;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Commands.SendNotification;

internal static class NotificationMessages
{
    public static string For(NotificationType type, Guid orderId, decimal totalAmount)
    {
        // Explicit format on the invariant culture: container images run in globalization-invariant mode,
        // where asking for a specific culture (e.g. en-US) throws.
        var total = "$" + totalAmount.ToString("N2", CultureInfo.InvariantCulture);

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
