using Inventory.Domain.Common;

namespace Inventory.Domain.Products.Events;

public sealed record ProductLowStockEvent(Guid ProductId, int QuantityOnHand, int ReorderLevel) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
