using Orders.Domain.Common;

namespace Orders.Domain.Orders.Events;

public sealed record OrderConfirmedEvent(Guid OrderId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
