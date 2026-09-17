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
        var command = new CreateProductTypeCommand("Hardware", "Physical hardware products");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Name.ShouldBe("Hardware");
        _repository.Verify(r => r.Add(It.IsAny<ProductType>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
