using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(ICategoryRepository repository)
    : IRequestHandler<GetCategoryByIdQuery, CategoryDetailsDto>
{
    public async Task<CategoryDetailsDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Categories.Category), request.Id);

        return CategoryDetailsDto.FromDomain(category);
    }
}
