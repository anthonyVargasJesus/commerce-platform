using Moq;
using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Common.Security;
using Orders.Domain.Customers;
using Orders.Domain.Orders;
using Shouldly;

namespace Orders.UnitTests.Application;

public class OrderAccessPolicyTests
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ICustomerRepository> _customers = new();

    private OrderAccessPolicy CreatePolicy() => new(_currentUser.Object, _customers.Object);

    private static Order OrderFor(Guid customerId) =>
        Order.Create(customerId, [OrderItem.Create(Guid.NewGuid(), "SKU", "Widget", 10m, 1)]);

    [Fact]
    public async Task GetScope_ForAdminsAndServices_ShouldBeUnrestrictedWithoutLookingUpACustomer()
    {
        _currentUser.SetupGet(u => u.CanAccessAllOrders).Returns(true);

        var scope = await CreatePolicy().GetScopeAsync(CancellationToken.None);

        scope.IsUnrestricted.ShouldBeTrue();
        _customers.Verify(c => c.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetScope_ForACustomerWithAProfile_ShouldBeLimitedToThatCustomer()
    {
        var customer = Customer.Create("Maria", "maria@example.com");
        _currentUser.SetupGet(u => u.Email).Returns("maria@example.com");
        _customers.Setup(c => c.GetByEmailAsync("maria@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var scope = await CreatePolicy().GetScopeAsync(CancellationToken.None);

        scope.IsUnrestricted.ShouldBeFalse();
        scope.CustomerId.ShouldBe(customer.Id);
        scope.Allows(customer.Id).ShouldBeTrue();
        scope.Allows(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public async Task GetScope_ForAUserWithoutACustomerProfile_ShouldSeeNothing()
    {
        _currentUser.SetupGet(u => u.Email).Returns("nobody@example.com");
        _customers.Setup(c => c.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var scope = await CreatePolicy().GetScopeAsync(CancellationToken.None);

        scope.HasNoAccess.ShouldBeTrue();
        scope.Allows(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public async Task GetScope_ForATokenWithoutEmail_ShouldSeeNothing()
    {
        _currentUser.SetupGet(u => u.Email).Returns((string?)null);

        var scope = await CreatePolicy().GetScopeAsync(CancellationToken.None);

        scope.HasNoAccess.ShouldBeTrue();
    }

    [Fact]
    public async Task EnsureCanAccess_ForAnotherCustomersOrder_ShouldThrowNotFound()
    {
        var mine = Customer.Create("Maria", "maria@example.com");
        _currentUser.SetupGet(u => u.Email).Returns("maria@example.com");
        _customers.Setup(c => c.GetByEmailAsync("maria@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(mine);
        var policy = CreatePolicy();

        await policy.EnsureCanAccessAsync(OrderFor(mine.Id), CancellationToken.None);
        await Should.ThrowAsync<NotFoundException>(() => policy.EnsureCanAccessAsync(OrderFor(Guid.NewGuid()), CancellationToken.None));
    }
}
