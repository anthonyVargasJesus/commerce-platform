using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Domain.Categories;
using Moq;
using Shouldly;

namespace Inventory.UnitTests.Application;

public class UpdateCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateCategoryCommandHandler CreateHandler() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenCategoryExists_ShouldUpdateAndPersist()
    {
        var category = Category.Create("Electronics");
        _repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        await CreateHandler().Handle(new UpdateCategoryCommand(category.Id, "Consumer Electronics", "Updated"), CancellationToken.None);

        category.Name.ShouldBe("Consumer Electronics");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => CreateHandler().Handle(new UpdateCategoryCommand(Guid.NewGuid(), "Name", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNameBelongsToAnotherCategory_ShouldThrowConflictException()
    {
        var category = Category.Create("Electronics");
        _repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _repository.Setup(r => r.NameExistsAsync("Furniture", category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Should.ThrowAsync<ConflictException>(
            () => CreateHandler().Handle(new UpdateCategoryCommand(category.Id, "Furniture", null), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNameIsUnchanged_ShouldNotQueryNameExistence()
    {
        var category = Category.Create("Electronics");
        _repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        await CreateHandler().Handle(new UpdateCategoryCommand(category.Id, "Electronics", "Updated description"), CancellationToken.None);

        _repository.Verify(r => r.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
