using Orders.Domain.Exceptions;
using Orders.Domain.Orders;
using Shouldly;

namespace Orders.UnitTests.Domain;

public class OrderItemTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateItem()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "SKU-1", "Widget", 10m, 3);

        item.LineTotal.ShouldBe(30m);
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => OrderItem.Create(Guid.NewGuid(), "SKU-1", "Widget", -1m, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveQuantity_ShouldThrowDomainException(int quantity)
    {
        Should.Throw<DomainException>(() => OrderItem.Create(Guid.NewGuid(), "SKU-1", "Widget", 10m, quantity));
    }
}
