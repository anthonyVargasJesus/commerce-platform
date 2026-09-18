using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Commands.AdjustStock;
using Inventory.Domain.Products;
using Moq;
using Shouldly;

namespace Inventory.UnitTests.Application;

public class AdjustStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IIdempotencyStore> _idempotencyStore = new();

    private AdjustStockCommandHandler CreateHandler() => new(_repository.Object, _idempotencyStore.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenProductExists_ShouldAdjustStockAndPersist()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m, 10, 2);
        _repository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await CreateHandler().Handle(new AdjustStockCommand(product.Id, 5), CancellationToken.None);

        result.QuantityOnHand.ShouldBe(15);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithANewIdempotencyKey_ShouldApplyTheChangeAndRememberTheKey()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m, 10, 2);
        _repository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _idempotencyStore.Setup(s => s.HasProcessedAsync("key-1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateHandler().Handle(new AdjustStockCommand(product.Id, -3, "key-1"), CancellationToken.None);

        result.QuantityOnHand.ShouldBe(7);
        _idempotencyStore.Verify(s => s.MarkProcessed("key-1"), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheKeyWasAlreadyProcessed_ShouldNotApplyTheChangeAgain()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m, 10, 2);
        _repository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _idempotencyStore.Setup(s => s.HasProcessedAsync("key-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateHandler().Handle(new AdjustStockCommand(product.Id, -3, "key-1"), CancellationToken.None);

        result.QuantityOnHand.ShouldBe(10);
        _idempotencyStore.Verify(s => s.MarkProcessed(It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutAKey_ShouldNotTouchTheIdempotencyStore()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m, 10, 2);
        _repository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await CreateHandler().Handle(new AdjustStockCommand(product.Id, 1), CancellationToken.None);

        _idempotencyStore.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => CreateHandler().Handle(new AdjustStockCommand(Guid.NewGuid(), 5), CancellationToken.None));
    }
}
