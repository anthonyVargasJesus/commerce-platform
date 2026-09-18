using Commerce.Contracts.Inventory;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Inventory.Domain.Products.Events;

namespace Inventory.Infrastructure.Messaging;

public static class ProductIntegrationEventMapper
{
    public static object Map(Product product, IDomainEvent domainEvent) => domainEvent switch
    {
        ProductCreatedEvent e => new ProductCreated(product.Id, product.Sku.Value, product.Name, e.OccurredOn),
        StockAdjustedEvent e => new StockAdjusted(product.Id, product.Sku.Value, e.Delta, e.NewQuantity, e.OccurredOn),
        ProductLowStockEvent e => new ProductLowStock(product.Id, product.Sku.Value, product.Name, e.QuantityOnHand, e.ReorderLevel, e.OccurredOn),
        _ => throw new NotSupportedException($"Domain event '{domainEvent.GetType().Name}' has no integration event mapping."),
    };
}
