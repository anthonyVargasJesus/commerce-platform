using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Categories;
using MediatR;

namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, CreatedCategoryDto>
{
    public async Task<CreatedCategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await repository.NameExistsAsync(name, excludingId: null, cancellationToken))
        {
            throw new ConflictException($"A category with name '{name}' already exists.");
        }

        var category = Category.Create(name, request.Description);

        repository.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedCategoryDto.FromDomain(category);
    }
}
