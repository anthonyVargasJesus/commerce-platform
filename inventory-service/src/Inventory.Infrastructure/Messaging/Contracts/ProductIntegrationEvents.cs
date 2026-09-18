namespace Commerce.Contracts.Inventory;

// Message contracts are matched by MassTransit on full type name (namespace + name), so every
// service that consumes these events declares its own copy under the same namespace.
public sealed record ProductCreated(Guid ProductId, string Sku, string Name, DateTimeOffset OccurredOn);

public sealed record StockAdjusted(Guid ProductId, string Sku, int Delta, int NewQuantity, DateTimeOffset OccurredOn);

public sealed record ProductLowStock(Guid ProductId, string Sku, string Name, int QuantityOnHand, int ReorderLevel, DateTimeOffset OccurredOn);
