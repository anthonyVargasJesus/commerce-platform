using System.Globalization;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Commands.SendNotification;

internal static class NotificationMessages
{
    public static string For(NotificationType type, string customerName, Guid orderId, decimal totalAmount)
    {
        // Explicit format on the invariant culture: container images run in globalization-invariant mode,
        // where asking for a specific culture (e.g. en-US) throws.
        var total = "$" + totalAmount.ToString("N2", CultureInfo.InvariantCulture);

        return type switch
        {
            NotificationType.OrderCreated => $"Hi {customerName}, we received your order {orderId} ({total}).",
            NotificationType.OrderConfirmed => $"Hi {customerName}, your order {orderId} ({total}) has been confirmed.",
            NotificationType.OrderShipped => $"Hi {customerName}, your order {orderId} is on its way.",
            NotificationType.OrderDelivered => $"Hi {customerName}, your order {orderId} was delivered.",
            NotificationType.OrderCancelled => $"Hi {customerName}, your order {orderId} ({total}) has been cancelled.",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown notification type."),
        };
    }
}
