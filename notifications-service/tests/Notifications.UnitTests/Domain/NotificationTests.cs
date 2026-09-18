using Notifications.Domain.Exceptions;
using Notifications.Domain.Notifications;
using Shouldly;

namespace Notifications.UnitTests.Domain;

public class NotificationTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateNotification()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var notification = Notification.Create(orderId, customerId, "jane@example.com", NotificationType.OrderConfirmed, "  Confirmed  ");

        notification.OrderId.ShouldBe(orderId);
        notification.CustomerId.ShouldBe(customerId);
        notification.Recipient.ShouldBe("jane@example.com");
        notification.Type.ShouldBe(NotificationType.OrderConfirmed);
        notification.Message.ShouldBe("Confirmed");
    }

    [Fact]
    public void Create_WithEmptyOrderId_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Notification.Create(Guid.Empty, Guid.NewGuid(), "jane@example.com", NotificationType.OrderCreated, "msg"));
    }

    [Fact]
    public void Create_WithEmptyCustomerId_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.Empty, "jane@example.com", NotificationType.OrderCreated, "msg"));
    }

    [Fact]
    public void Create_WithEmptyRecipient_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), " ", NotificationType.OrderCreated, "msg"));
    }

    [Fact]
    public void Create_WithEmptyMessage_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "jane@example.com", NotificationType.OrderCreated, " "));
    }

    [Fact]
    public void CreateStockAlert_WithValidData_ShouldCreateLowStockNotificationForTheProduct()
    {
        var productId = Guid.NewGuid();

        var notification = Notification.CreateStockAlert(productId, "Low stock");

        notification.Type.ShouldBe(NotificationType.LowStock);
        notification.ProductId.ShouldBe(productId);
        notification.OrderId.ShouldBeNull();
        notification.CustomerId.ShouldBeNull();
    }

    [Fact]
    public void CreateStockAlert_WithEmptyProductId_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Notification.CreateStockAlert(Guid.Empty, "Low stock"));
    }
}
