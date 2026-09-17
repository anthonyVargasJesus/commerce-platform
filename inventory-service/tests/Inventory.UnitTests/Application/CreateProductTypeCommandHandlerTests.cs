using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.ProductTypes.Commands.CreateProductType;
using Inventory.Domain.ProductTypes;
using Moq;
using Shouldly;

namespace Inventory.UnitTests.Application;

public class CreateProductTypeCommandHandlerTests
{
    private readonly Mock<IProductTypeRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateProductTypeCommandHandler CreateHandler() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithValidData_ShouldAddProductTypeAndReturnDto()
    {
        _repository.Setup(r => r.NameExistsAsync("Hardware", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateProductTypeCommand("Hardware", "Physical hardware products");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Name.ShouldBe("Hardware");
        _repository.Verify(r => r.Add(It.IsAny<ProductType>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingName_ShouldThrowConflictException()
    {
        _repository.Setup(r => r.NameExistsAsync("Hardware", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new CreateProductTypeCommand("Hardware", null);

        await Should.ThrowAsync<ConflictException>(() => CreateHandler().Handle(command, CancellationToken.None));

        _repository.Verify(r => r.Add(It.IsAny<ProductType>()), Times.Never);
    }
}
