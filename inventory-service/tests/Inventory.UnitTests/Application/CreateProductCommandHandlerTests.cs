using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Domain.Products;
using Moq;
using Shouldly;

namespace Inventory.UnitTests.Application;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IProductTypeRepository> _productTypeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateProductCommandHandler CreateHandler() => new(_repository.Object, _categoryRepository.Object, _productTypeRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithNewSku_ShouldAddProductAndReturnDto()
    {
        _repository.Setup(r => r.SkuExistsAsync("sku-1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateProductCommand("sku-1", "Widget", "A widget", 9.99m, 10, 2);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Sku.ShouldBe("SKU-1");
        result.Name.ShouldBe("Widget");
        _repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingSku_ShouldThrowConflictException()
    {
        _repository.Setup(r => r.SkuExistsAsync("sku-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new CreateProductCommand("sku-1", "Widget", null, 9.99m, 10, 2);

        await Should.ThrowAsync<ConflictException>(() => CreateHandler().Handle(command, CancellationToken.None));

        _repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }
}
