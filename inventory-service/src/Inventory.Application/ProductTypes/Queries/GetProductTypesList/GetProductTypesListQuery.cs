using MediatR;

namespace Inventory.Application.ProductTypes.Queries.GetProductTypesList;

public sealed record GetProductTypesListQuery : IRequest<IReadOnlyList<ProductTypeListItemDto>>;
