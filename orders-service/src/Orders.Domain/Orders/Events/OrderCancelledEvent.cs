using Orders.Domain.Common;

namespace Orders.Domain.Orders.Events;

public sealed record OrderCancelledEvent(Guid OrderId, OrderStatus PreviousStatus) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
