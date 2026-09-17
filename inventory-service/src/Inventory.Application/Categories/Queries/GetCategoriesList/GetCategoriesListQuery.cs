using MediatR;

namespace Inventory.Application.Categories.Queries.GetCategoriesList;

public sealed record GetCategoriesListQuery : IRequest<IReadOnlyList<CategoryListItemDto>>;
