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

        category.UpdateDetails(request.Name, request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
