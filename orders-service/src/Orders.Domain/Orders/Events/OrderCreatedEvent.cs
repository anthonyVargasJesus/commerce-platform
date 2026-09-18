using Orders.Domain.Common;

namespace Orders.Domain.Orders.Events;

public sealed record OrderCreatedEvent(Guid OrderId, Guid CustomerId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
