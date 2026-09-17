using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Categories.Queries.GetCategoriesList;

public sealed class GetCategoriesListQueryHandler(ICategoryRepository repository)
    : IRequestHandler<GetCategoriesListQuery, IReadOnlyList<CategoryListItemDto>>
{
    public async Task<IReadOnlyList<CategoryListItemDto>> Handle(GetCategoriesListQuery request, CancellationToken cancellationToken)
    {
        var categories = await repository.GetAllAsync(cancellationToken);

        return categories.Select(CategoryListItemDto.FromDomain).ToList();
    }
}
