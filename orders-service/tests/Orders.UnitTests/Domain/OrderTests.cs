using Orders.Domain.Exceptions;
using Orders.Domain.Orders;
using Orders.Domain.Orders.Events;
using Shouldly;

namespace Orders.UnitTests.Domain;

public class OrderTests
{
    private static OrderItem CreateItem(decimal unitPrice = 10m, int quantity = 2) =>
        OrderItem.Create(Guid.NewGuid(), "SKU-1", "Widget", unitPrice, quantity);

    [Fact]
    public void Create_WithValidItems_ShouldCreatePendingOrderAndRaiseCreatedEvent()
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem()]);

        order.Status.ShouldBe(OrderStatus.Pending);
        order.Items.Count.ShouldBe(1);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderCreatedEvent>();
    }

    [Fact]
    public void Create_WithNoItems_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Order.Create(Guid.NewGuid(), []));
    }

    [Fact]
    public void TotalAmount_ShouldSumAllLineTotals()
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem(10m, 2), CreateItem(5m, 3)]);

        order.TotalAmount.ShouldBe(35m);
    }

    [Fact]
    public void Confirm_WhenPending_ShouldTransitionToConfirmed()
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem()]);
        order.ClearDomainEvents();

        order.Confirm();

        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderConfirmedEvent>();
    }

    [Fact]
    public void Confirm_WhenNotPending_ShouldThrowInvalidOrderStateTransitionException()
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem()]);
        order.Confirm();

        Should.Throw<InvalidOrderStateTransitionException>(() => order.Confirm());
    }

    [Fact]
    public void Ship_WhenConfirmed_ShouldTransitionToShipped()
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem()]);
        order.Confirm();

        order.Ship();

        order.Status.ShouldBe(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_WhenPending_ShouldThrowInvalidOrderStateTransitionException()
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem()]);

        Should.Throw<InvalidOrderStateTransitionException>(() => order.Ship());
    }

    [Fact]
    public void Deliver_WhenShipped_ShouldTransitionToDelivered()
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem()]);
        order.Confirm();
        order.Ship();

        order.Deliver();

        order.Status.ShouldBe(OrderStatus.Delivered);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public void Cancel_WhenPendingOrConfirmed_ShouldTransitionToCancelled(OrderStatus status)
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem()]);
        if (status == OrderStatus.Confirmed)
        {
            order.Confirm();
        }
        order.ClearDomainEvents();

        order.Cancel();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        var raised = order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderCancelledEvent>();
        raised.PreviousStatus.ShouldBe(status);
    }

    [Fact]
    public void Cancel_WhenShipped_ShouldThrowInvalidOrderStateTransitionException()
    {
        var order = Order.Create(Guid.NewGuid(), [CreateItem()]);
        order.Confirm();
        order.Ship();

        Should.Throw<InvalidOrderStateTransitionException>(() => order.Cancel());
    }
}
