using Orders.Domain.Common;
using Orders.Domain.Exceptions;
using Orders.Domain.Orders.Events;

namespace Orders.Domain.Orders;

public sealed class Order : BaseAuditableEntity
{
    private readonly List<OrderItem> _items = [];

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(i => i.LineTotal);

    private Order()
    {
    }

    public static Order Create(Guid customerId, IEnumerable<OrderItem> items)
    {
        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            throw new DomainException("An order must have at least one item.");
        }

        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.Pending,
        };

        order._items.AddRange(itemList);

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, order.CustomerId));

        return order;
    }

    public void Confirm()
    {
        EnsureStatus(OrderStatus.Pending, nameof(Confirm));

        Status = OrderStatus.Confirmed;

        AddDomainEvent(new OrderConfirmedEvent(Id));
    }

    public void Ship()
    {
        EnsureStatus(OrderStatus.Confirmed, nameof(Ship));

        Status = OrderStatus.Shipped;

        AddDomainEvent(new OrderShippedEvent(Id));
    }

    public void Deliver()
    {
        EnsureStatus(OrderStatus.Shipped, nameof(Deliver));

        Status = OrderStatus.Delivered;

        AddDomainEvent(new OrderDeliveredEvent(Id));
    }

    public void Cancel()
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
        {
            throw new InvalidOrderStateTransitionException(Status, nameof(Cancel));
        }

        var previousStatus = Status;
        Status = OrderStatus.Cancelled;

        AddDomainEvent(new OrderCancelledEvent(Id, previousStatus));
    }

    private void EnsureStatus(OrderStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOrderStateTransitionException(Status, action);
        }
    }
}
