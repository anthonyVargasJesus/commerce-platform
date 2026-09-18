using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Common.Security;
using Orders.Application.Orders.Commands.CancelOrder;
using Orders.Domain.Orders;
using Moq;
using Shouldly;

namespace Orders.UnitTests.Application;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<IInventoryServiceClient> _inventoryClient = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderAccessPolicy> _accessPolicy = new();

    private CancelOrderCommandHandler CreateHandler() => new(_repository.Object, _accessPolicy.Object, _inventoryClient.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenRestocking_ShouldSendADistinctKeyPerItem()
    {
        var order = Order.Create(Guid.NewGuid(), [OrderItem.Create(Guid.NewGuid(), "A", "A", 10m, 1), OrderItem.Create(Guid.NewGuid(), "B", "B", 10m, 2)]);
        order.Confirm();
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var keys = new List<string>();
        _inventoryClient
            .Setup(c => c.AdjustStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, int, string, CancellationToken>((_, _, key, _) => keys.Add(key))
            .ReturnsAsync(InventoryAdjustmentResult.Success);

        await CreateHandler().Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        keys.Count.ShouldBe(2);
        keys.ShouldAllBe(key => !string.IsNullOrWhiteSpace(key));
        keys.Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenTheOrderIsNotVisibleToTheUser_ShouldNotCancelOrRestock()
    {
        var order = Order.Create(Guid.NewGuid(), [OrderItem.Create(Guid.NewGuid(), "SKU", "Widget", 10m, 3)]);
        order.Confirm();
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _accessPolicy
            .Setup(p => p.EnsureCanAccessAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Order", order.Id));

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(new CancelOrderCommand(order.Id), CancellationToken.None));

        order.Status.ShouldBe(OrderStatus.Confirmed);
        _inventoryClient.Verify(c => c.AdjustStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderWasConfirmed_ShouldRestockItemsInInventory()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), [OrderItem.Create(productId, "SKU", "Widget", 10m, 3)]);
        order.Confirm();
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateHandler().Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.ShouldBe(OrderStatus.Cancelled);
        _inventoryClient.Verify(c => c.AdjustStockAsync(productId, 3, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderWasPending_ShouldNotCallInventory()
    {
        var order = Order.Create(Guid.NewGuid(), [OrderItem.Create(Guid.NewGuid(), "SKU", "Widget", 10m, 1)]);
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateHandler().Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.ShouldBe(OrderStatus.Cancelled);
        _inventoryClient.Verify(c => c.AdjustStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(new CancelOrderCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
