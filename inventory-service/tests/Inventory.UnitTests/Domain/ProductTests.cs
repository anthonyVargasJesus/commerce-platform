using Inventory.Domain.Exceptions;
using Inventory.Domain.Products;
using Inventory.Domain.Products.Events;
using Shouldly;

namespace Inventory.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateProductAndRaiseCreatedEvent()
    {
        var product = Product.Create("sku-1", "Widget", 9.99m, 10, 2);

        product.Sku.Value.ShouldBe("SKU-1");
        product.Name.ShouldBe("Widget");
        product.QuantityOnHand.ShouldBe(10);
        product.IsActive.ShouldBeTrue();
        product.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ProductCreatedEvent>();
    }

    [Theory]
    [InlineData("", "Widget", 1, 1, 1)]
    [InlineData("SKU-1", "", 1, 1, 1)]
    [InlineData("SKU-1", "Widget", -1, 1, 1)]
    [InlineData("SKU-1", "Widget", 1, -1, 1)]
    [InlineData("SKU-1", "Widget", 1, 1, -1)]
    public void Create_WithInvalidData_ShouldThrowDomainException(string sku, string name, decimal price, int quantity, int reorderLevel)
    {
        Should.Throw<DomainException>(() => Product.Create(sku, name, price, quantity, reorderLevel));
    }

    [Fact]
    public void AdjustStock_WithPositiveDelta_ShouldIncreaseQuantityAndRaiseEvent()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m, 10, 2);
        product.ClearDomainEvents();

        product.AdjustStock(5);

        product.QuantityOnHand.ShouldBe(15);
        product.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<StockAdjustedEvent>();
    }

    [Fact]
    public void AdjustStock_WhenResultingQuantityIsBelowZero_ShouldThrowInsufficientStockException()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m, 10, 2);

        Should.Throw<InsufficientStockException>(() => product.AdjustStock(-11));
    }

    [Fact]
    public void AdjustStock_WhenQuantityFallsToOrBelowReorderLevel_ShouldRaiseLowStockEvent()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m, 10, 5);
        product.ClearDomainEvents();

        product.AdjustStock(-6);

        product.DomainEvents.OfType<ProductLowStockEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m, 10, 2);

        product.Deactivate();

        product.IsActive.ShouldBeFalse();
    }
}
