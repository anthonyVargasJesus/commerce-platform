using Inventory.Domain.Common;

namespace Inventory.Domain.Products.Events;

public sealed record ProductCreatedEvent(Guid ProductId, string Sku) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
