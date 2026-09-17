using Inventory.Domain.Exceptions;
using Inventory.Domain.ProductTypes;
using Shouldly;

namespace Inventory.UnitTests.Domain;

public class ProductTypeTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateProductType()
    {
        var productType = ProductType.Create("Hardware", "Physical hardware products");

        productType.Name.ShouldBe("Hardware");
        productType.Description.ShouldBe("Physical hardware products");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => ProductType.Create(""));
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateNameAndDescription()
    {
        var productType = ProductType.Create("Hardware");

        productType.UpdateDetails("Software", "Updated description");

        productType.Name.ShouldBe("Software");
        productType.Description.ShouldBe("Updated description");
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_ShouldThrowDomainException()
    {
        var productType = ProductType.Create("Hardware");

        Should.Throw<DomainException>(() => productType.UpdateDetails("", null));
    }
}
