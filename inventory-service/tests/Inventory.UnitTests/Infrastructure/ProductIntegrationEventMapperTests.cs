using Commerce.Contracts.Inventory;
using Inventory.Domain.Products;
using Inventory.Domain.Products.Events;
using Inventory.Infrastructure.Messaging;
using Shouldly;

namespace Inventory.UnitTests.Infrastructure;

public class ProductIntegrationEventMapperTests
{
    private static Product CreateProduct(int quantity = 10, int reorderLevel = 5) =>
        Product.Create("sku-1", "Widget", 9.99m, quantity, reorderLevel);

    [Fact]
    public void Map_ProductLowStockEvent_ShouldCarryProductDataAndThresholds()
    {
        var product = CreateProduct();
        var domainEvent = new ProductLowStockEvent(product.Id, 3, 5);

        var message = ProductIntegrationEventMapper.Map(product, domainEvent);

        var lowStock = message.ShouldBeOfType<ProductLowStock>();
        lowStock.ProductId.ShouldBe(product.Id);
        lowStock.Sku.ShouldBe("SKU-1");
        lowStock.Name.ShouldBe("Widget");
        lowStock.QuantityOnHand.ShouldBe(3);
        lowStock.ReorderLevel.ShouldBe(5);
    }

    [Fact]
    public void Map_EveryEventRaisedByProduct_ShouldHaveAnIntegrationEvent()
    {
        var product = CreateProduct(quantity: 6, reorderLevel: 5);
        product.AdjustStock(-3);

        product.DomainEvents.Count.ShouldBeGreaterThanOrEqualTo(3);
        foreach (var domainEvent in product.DomainEvents)
        {
            Should.NotThrow(() => ProductIntegrationEventMapper.Map(product, domainEvent));
        }
    }
}
