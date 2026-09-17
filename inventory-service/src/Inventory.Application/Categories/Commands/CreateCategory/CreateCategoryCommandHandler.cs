using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Categories;
using MediatR;

namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, CreatedCategoryDto>
{
    public async Task<CreatedCategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.Name, request.Description);

        repository.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedCategoryDto.FromDomain(category);
    }
}
