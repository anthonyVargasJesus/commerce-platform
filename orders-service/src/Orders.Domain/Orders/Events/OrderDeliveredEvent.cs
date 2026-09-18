using Orders.Domain.Common;

namespace Orders.Domain.Orders.Events;

public sealed record OrderDeliveredEvent(Guid OrderId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
