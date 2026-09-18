namespace Commerce.Contracts.Inventory;

// Consumer-side copy of the contracts published by inventory-service (only the events this service
// reacts to). MassTransit matches messages by full type name, so this must stay identical to the
// publisher's declaration.
public sealed record ProductLowStock(Guid ProductId, string Sku, string Name, int QuantityOnHand, int ReorderLevel, DateTimeOffset OccurredOn);
