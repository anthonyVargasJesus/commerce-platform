using Orders.Domain.Customers;
using Orders.Domain.Exceptions;
using Shouldly;

namespace Orders.UnitTests.Domain;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCustomer()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com", "555-1234");

        customer.Name.ShouldBe("Jane Doe");
        customer.Email.ShouldBe("jane@example.com");
        customer.Phone.ShouldBe("555-1234");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Customer.Create("", "jane@example.com"));
    }

    [Fact]
    public void Create_WithEmptyEmail_ShouldThrowDomainException()
    {
        Should.Throw<DomainException>(() => Customer.Create("Jane Doe", ""));
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateFields()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com");

        customer.UpdateDetails("Jane Smith", "jane.smith@example.com", "555-5678");

        customer.Name.ShouldBe("Jane Smith");
        customer.Email.ShouldBe("jane.smith@example.com");
        customer.Phone.ShouldBe("555-5678");
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_ShouldThrowDomainException()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com");

        Should.Throw<DomainException>(() => customer.UpdateDetails("", "jane@example.com", null));
    }
}
