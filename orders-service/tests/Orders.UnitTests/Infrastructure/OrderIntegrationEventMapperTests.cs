using Commerce.Contracts.Orders;
using Orders.Domain.Customers;
using Orders.Domain.Orders;
using Orders.Domain.Orders.Events;
using Orders.Infrastructure.Messaging;
using Shouldly;

namespace Orders.UnitTests.Infrastructure;

public class OrderIntegrationEventMapperTests
{
    private static readonly Customer Customer = Customer.Create("Jane Doe", "jane@example.com");

    private static Order CreateOrder() =>
        Order.Create(Guid.NewGuid(), [OrderItem.Create(Guid.NewGuid(), "SKU-1", "Widget", 10m, 3)]);

    [Fact]
    public void Map_OrderConfirmedEvent_ShouldProduceOrderConfirmedWithOrderData()
    {
        var order = CreateOrder();
        var domainEvent = new OrderConfirmedEvent(order.Id);

        var message = OrderIntegrationEventMapper.Map(order, Customer, domainEvent);

        var confirmed = message.ShouldBeOfType<OrderConfirmed>();
        confirmed.OrderId.ShouldBe(order.Id);
        confirmed.CustomerId.ShouldBe(order.CustomerId);
        confirmed.CustomerName.ShouldBe("Jane Doe");
        confirmed.CustomerEmail.ShouldBe("jane@example.com");
        confirmed.TotalAmount.ShouldBe(30m);
        confirmed.OccurredOn.ShouldBe(domainEvent.OccurredOn);
    }

    [Fact]
    public void Map_OrderCancelledEvent_ShouldProduceOrderCancelled()
    {
        var order = CreateOrder();

        var message = OrderIntegrationEventMapper.Map(order, Customer, new OrderCancelledEvent(order.Id, OrderStatus.Confirmed));

        message.ShouldBeOfType<OrderCancelled>().OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public void Map_EveryEventRaisedByOrder_ShouldHaveAnIntegrationEvent()
    {
        var order = CreateOrder();
        order.Confirm();
        order.Ship();
        order.Deliver();

        foreach (var domainEvent in order.DomainEvents)
        {
            Should.NotThrow(() => OrderIntegrationEventMapper.Map(order, Customer, domainEvent));
        }
    }
}
