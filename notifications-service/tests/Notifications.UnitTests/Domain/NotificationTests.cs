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

        var notification = Notification.Create(orderId, customerId, NotificationType.OrderConfirmed, "  Confirmed  ");

        notification.OrderId.ShouldBe(orderId);
        notification.CustomerId.ShouldBe(customerId);
        notification.Type.ShouldBe(NotificationType.OrderConfirmed);
        notification.Message.ShouldBe("Confirmed");
    }

    [Fact]
    public void Create_WithEmptyOrderId_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Notification.Create(Guid.Empty, Guid.NewGuid(), NotificationType.OrderCreated, "msg"));
    }

    [Fact]
    public void Create_WithEmptyCustomerId_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.Empty, NotificationType.OrderCreated, "msg"));
    }

    [Fact]
    public void Create_WithEmptyMessage_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.OrderCreated, " "));
    }
}
