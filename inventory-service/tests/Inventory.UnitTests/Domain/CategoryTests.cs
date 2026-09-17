using Inventory.Domain.Categories;
using Inventory.Domain.Exceptions;
using Shouldly;

namespace Inventory.UnitTests.Domain;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCategory()
    {
        var category = Category.Create("Electronics", "Electronic devices");

        category.Name.ShouldBe("Electronics");
        category.Description.ShouldBe("Electronic devices");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Category.Create(""));
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateNameAndDescription()
    {
        var category = Category.Create("Electronics");

        category.UpdateDetails("Consumer Electronics", "Updated description");

        category.Name.ShouldBe("Consumer Electronics");
        category.Description.ShouldBe("Updated description");
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_ShouldThrowDomainException()
    {
        var category = Category.Create("Electronics");

        Should.Throw<DomainException>(() => category.UpdateDetails("", null));
    }
}
