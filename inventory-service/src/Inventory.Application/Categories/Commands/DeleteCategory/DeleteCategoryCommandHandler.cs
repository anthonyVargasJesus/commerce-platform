using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Categories.Category), request.Id);

        if (await productRepository.ExistsForCategoryAsync(request.Id, cancellationToken))
        {
            throw new ConflictException($"Category '{category.Name}' cannot be deleted because it still has products assigned to it.");
        }

        categoryRepository.Remove(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
