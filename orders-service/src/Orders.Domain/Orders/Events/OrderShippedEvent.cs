using Orders.Domain.Common;

namespace Orders.Domain.Orders.Events;

public sealed record OrderShippedEvent(Guid OrderId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
