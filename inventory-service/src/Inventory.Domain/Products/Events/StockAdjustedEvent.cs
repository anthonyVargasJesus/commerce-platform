using Inventory.Domain.Common;

namespace Inventory.Domain.Products.Events;

public sealed record StockAdjustedEvent(Guid ProductId, int Delta, int NewQuantity) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
