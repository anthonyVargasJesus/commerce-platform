using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Categories.Category), request.Id);

        var name = request.Name.Trim();

        if (!string.Equals(name, category.Name, StringComparison.Ordinal)
            && await repository.NameExistsAsync(name, excludingId: request.Id, cancellationToken))
        {
            throw new ConflictException($"A category with name '{name}' already exists.");
        }

        category.UpdateDetails(name, request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
