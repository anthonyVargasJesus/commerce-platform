using Moq;
using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Common.Security;
using Orders.Application.Orders.Queries.GetOrderById;
using Orders.Application.Orders.Queries.GetOrdersList;
using Orders.Domain.Orders;
using Shouldly;

namespace Orders.UnitTests.Application;

public class OrderQueryHandlersScopeTests
{
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<IOrderAccessPolicy> _accessPolicy = new();

    [Fact]
    public async Task List_ForAUserWithoutAProfile_ShouldReturnAnEmptyPageWithoutQueryingOrders()
    {
        _accessPolicy.Setup(p => p.GetScopeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(OrderScope.Nothing);

        var page = await new GetOrdersListQueryHandler(_repository.Object, _accessPolicy.Object)
            .Handle(new GetOrdersListQuery(), CancellationToken.None);

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
        _repository.Verify(r => r.GetPagedAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task List_ForACustomer_ShouldFilterByThatCustomer()
    {
        var customerId = Guid.NewGuid();
        _accessPolicy.Setup(p => p.GetScopeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(OrderScope.OnlyCustomer(customerId));
        _repository
            .Setup(r => r.GetPagedAsync(customerId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Order>)[], 0));

        await new GetOrdersListQueryHandler(_repository.Object, _accessPolicy.Object).Handle(new GetOrdersListQuery(), CancellationToken.None);

        _repository.Verify(r => r.GetPagedAsync(customerId, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_ForAnAdmin_ShouldNotFilter()
    {
        _accessPolicy.Setup(p => p.GetScopeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(OrderScope.Unrestricted);
        _repository
            .Setup(r => r.GetPagedAsync(null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Order>)[], 0));

        await new GetOrdersListQueryHandler(_repository.Object, _accessPolicy.Object).Handle(new GetOrdersListQuery(), CancellationToken.None);

        _repository.Verify(r => r.GetPagedAsync(null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ForAnOrderTheUserCannotSee_ShouldThrowNotFound()
    {
        var order = Order.Create(Guid.NewGuid(), [OrderItem.Create(Guid.NewGuid(), "SKU", "Widget", 10m, 1)]);
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _accessPolicy
            .Setup(p => p.EnsureCanAccessAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Order", order.Id));

        await Should.ThrowAsync<NotFoundException>(
            () => new GetOrderByIdQueryHandler(_repository.Object, _accessPolicy.Object).Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None));
    }
}
