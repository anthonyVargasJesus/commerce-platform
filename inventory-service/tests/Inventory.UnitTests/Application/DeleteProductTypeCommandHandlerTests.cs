using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.ProductTypes.Commands.DeleteProductType;
using Inventory.Domain.ProductTypes;
using Moq;
using Shouldly;

namespace Inventory.UnitTests.Application;

public class DeleteProductTypeCommandHandlerTests
{
    private readonly Mock<IProductTypeRepository> _productTypeRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteProductTypeCommandHandler CreateHandler() => new(_productTypeRepository.Object, _productRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenProductTypeExistsAndHasNoProducts_ShouldRemoveAndPersist()
    {
        var productType = ProductType.Create("Hardware");
        _productTypeRepository.Setup(r => r.GetByIdAsync(productType.Id, It.IsAny<CancellationToken>())).ReturnsAsync(productType);
        _productRepository.Setup(r => r.ExistsForProductTypeAsync(productType.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateHandler().Handle(new DeleteProductTypeCommand(productType.Id), CancellationToken.None);

        _productTypeRepository.Verify(r => r.Remove(productType), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductTypeHasProducts_ShouldThrowConflictException()
    {
        var productType = ProductType.Create("Hardware");
        _productTypeRepository.Setup(r => r.GetByIdAsync(productType.Id, It.IsAny<CancellationToken>())).ReturnsAsync(productType);
        _productRepository.Setup(r => r.ExistsForProductTypeAsync(productType.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Should.ThrowAsync<ConflictException>(() => CreateHandler().Handle(new DeleteProductTypeCommand(productType.Id), CancellationToken.None));

        _productTypeRepository.Verify(r => r.Remove(It.IsAny<ProductType>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductTypeDoesNotExist_ShouldThrowNotFoundException()
    {
        _productTypeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProductType?)null);

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(new DeleteProductTypeCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
