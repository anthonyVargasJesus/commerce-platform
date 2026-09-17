using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Categories;
using Moq;
using Shouldly;

namespace Inventory.UnitTests.Application;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteCategoryCommandHandler CreateHandler() => new(_categoryRepository.Object, _productRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenCategoryExistsAndHasNoProducts_ShouldRemoveAndPersist()
    {
        var category = Category.Create("Electronics");
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _productRepository.Setup(r => r.ExistsForCategoryAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateHandler().Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        _categoryRepository.Verify(r => r.Remove(category), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCategoryHasProducts_ShouldThrowConflictException()
    {
        var category = Category.Create("Electronics");
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _productRepository.Setup(r => r.ExistsForCategoryAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Should.ThrowAsync<ConflictException>(() => CreateHandler().Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None));

        _categoryRepository.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ShouldThrowNotFoundException()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
