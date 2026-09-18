using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
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

    private CancelOrderCommandHandler CreateHandler() => new(_repository.Object, _inventoryClient.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenOrderWasConfirmed_ShouldRestockItemsInInventory()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), [OrderItem.Create(productId, "SKU", "Widget", 10m, 3)]);
        order.Confirm();
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateHandler().Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.ShouldBe(OrderStatus.Cancelled);
        _inventoryClient.Verify(c => c.AdjustStockAsync(productId, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderWasPending_ShouldNotCallInventory()
    {
        var order = Order.Create(Guid.NewGuid(), [OrderItem.Create(Guid.NewGuid(), "SKU", "Widget", 10m, 1)]);
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateHandler().Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.ShouldBe(OrderStatus.Cancelled);
        _inventoryClient.Verify(c => c.AdjustStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(new CancelOrderCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
