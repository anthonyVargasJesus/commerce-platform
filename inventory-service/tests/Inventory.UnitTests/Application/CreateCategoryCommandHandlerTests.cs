using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Categories;
using Moq;
using Shouldly;

namespace Inventory.UnitTests.Application;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateCategoryCommandHandler CreateHandler() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithValidData_ShouldAddCategoryAndReturnDto()
    {
        _repository.Setup(r => r.NameExistsAsync("Electronics", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateCategoryCommand("Electronics", "Electronic devices");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Name.ShouldBe("Electronics");
        _repository.Verify(r => r.Add(It.IsAny<Category>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingName_ShouldThrowConflictException()
    {
        _repository.Setup(r => r.NameExistsAsync("Electronics", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new CreateCategoryCommand("Electronics", null);

        await Should.ThrowAsync<ConflictException>(() => CreateHandler().Handle(command, CancellationToken.None));

        _repository.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
    }
}
