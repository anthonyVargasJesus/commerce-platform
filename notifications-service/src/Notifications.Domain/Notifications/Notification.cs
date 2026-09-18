using Notifications.Domain.Common;
using Notifications.Domain.Exceptions;

namespace Notifications.Domain.Notifications;

public sealed class Notification : BaseAuditableEntity
{
    // Order notifications reference an order and a customer; stock alerts reference a product.
    // The other side of each pair stays null.
    public Guid? OrderId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? ProductId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Message { get; private set; } = string.Empty;

    private Notification()
    {
    }

    public static Notification Create(Guid orderId, Guid customerId, NotificationType type, string message)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Notification order id cannot be empty.");
        }

        if (customerId == Guid.Empty)
        {
            throw new DomainException("Notification customer id cannot be empty.");
        }

        EnsureMessage(message);

        return new Notification
        {
            OrderId = orderId,
            CustomerId = customerId,
            Type = type,
            Message = message.Trim(),
        };
    }

    public static Notification CreateStockAlert(Guid productId, string message)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Notification product id cannot be empty.");
        }

        EnsureMessage(message);

        return new Notification
        {
            ProductId = productId,
            Type = NotificationType.LowStock,
            Message = message.Trim(),
        };
    }

    private static void EnsureMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new DomainException("Notification message cannot be empty.");
        }
    }
}
