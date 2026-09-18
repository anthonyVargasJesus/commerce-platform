using Commerce.Contracts.Orders;
using Orders.Domain.Common;
using Orders.Domain.Customers;
using Orders.Domain.Orders;
using Orders.Domain.Orders.Events;

namespace Orders.Infrastructure.Messaging;

public static class OrderIntegrationEventMapper
{
    public static object Map(Order order, Customer customer, IDomainEvent domainEvent) => domainEvent switch
    {
        OrderCreatedEvent e => new OrderCreated(order.Id, order.CustomerId, customer.Name, customer.Email, order.TotalAmount, e.OccurredOn),
        OrderConfirmedEvent e => new OrderConfirmed(order.Id, order.CustomerId, customer.Name, customer.Email, order.TotalAmount, e.OccurredOn),
        OrderShippedEvent e => new OrderShipped(order.Id, order.CustomerId, customer.Name, customer.Email, order.TotalAmount, e.OccurredOn),
        OrderDeliveredEvent e => new OrderDelivered(order.Id, order.CustomerId, customer.Name, customer.Email, order.TotalAmount, e.OccurredOn),
        OrderCancelledEvent e => new OrderCancelled(order.Id, order.CustomerId, customer.Name, customer.Email, order.TotalAmount, e.OccurredOn),
        _ => throw new NotSupportedException($"Domain event '{domainEvent.GetType().Name}' has no integration event mapping."),
    };
}
