using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.ProductTypes.Commands.UpdateProductType;
using Inventory.Domain.ProductTypes;
using Moq;
using Shouldly;

namespace Inventory.UnitTests.Application;

public class UpdateProductTypeCommandHandlerTests
{
    private readonly Mock<IProductTypeRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateProductTypeCommandHandler CreateHandler() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenProductTypeExists_ShouldUpdateAndPersist()
    {
        var productType = ProductType.Create("Hardware");
        _repository.Setup(r => r.GetByIdAsync(productType.Id, It.IsAny<CancellationToken>())).ReturnsAsync(productType);

        await CreateHandler().Handle(new UpdateProductTypeCommand(productType.Id, "Software", "Updated"), CancellationToken.None);

        productType.Name.ShouldBe("Software");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductTypeDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProductType?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => CreateHandler().Handle(new UpdateProductTypeCommand(Guid.NewGuid(), "Name", null), CancellationToken.None));
    }
}
