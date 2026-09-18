using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Orders.Commands.ConfirmOrder;
using Orders.Domain.Orders;
using Moq;
using Shouldly;

namespace Orders.UnitTests.Application;

public class ConfirmOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<IInventoryServiceClient> _inventoryClient = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ConfirmOrderCommandHandler CreateHandler() => new(_repository.Object, _inventoryClient.Object, _unitOfWork.Object);

    private static Order CreateOrderWithItems(params (Guid ProductId, int Quantity)[] items)
    {
        var orderItems = items.Select(i => OrderItem.Create(i.ProductId, "SKU", "Widget", 10m, i.Quantity));
        return Order.Create(Guid.NewGuid(), orderItems);
    }

    [Fact]
    public async Task Handle_WhenAllItemsHaveStock_ShouldConfirmOrder()
    {
        var productId = Guid.NewGuid();
        var order = CreateOrderWithItems((productId, 2));
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _inventoryClient
            .Setup(c => c.AdjustStockAsync(productId, -2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryAdjustmentResult.Success);

        var result = await CreateHandler().Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        result.Status.ShouldBe(OrderStatus.Confirmed);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSecondItemHasInsufficientStock_ShouldCompensateFirstItemAndThrow()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var order = CreateOrderWithItems((productA, 2), (productB, 5));
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _inventoryClient
            .Setup(c => c.AdjustStockAsync(productA, -2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryAdjustmentResult.Success);
        _inventoryClient
            .Setup(c => c.AdjustStockAsync(productB, -5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryAdjustmentResult.InsufficientStock);

        await Should.ThrowAsync<ConflictException>(() => CreateHandler().Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None));

        // Compensation: productA's stock must be given back since it was already decremented.
        _inventoryClient.Verify(c => c.AdjustStockAsync(productA, 2, It.IsAny<CancellationToken>()), Times.Once);
        order.Status.ShouldBe(OrderStatus.Pending);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(new ConfirmOrderCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
