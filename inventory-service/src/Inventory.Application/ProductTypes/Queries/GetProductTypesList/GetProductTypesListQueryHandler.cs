using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.ProductTypes.Queries.GetProductTypesList;

public sealed class GetProductTypesListQueryHandler(IProductTypeRepository repository)
    : IRequestHandler<GetProductTypesListQuery, IReadOnlyList<ProductTypeListItemDto>>
{
    public async Task<IReadOnlyList<ProductTypeListItemDto>> Handle(GetProductTypesListQuery request, CancellationToken cancellationToken)
    {
        var productTypes = await repository.GetAllAsync(cancellationToken);

        return productTypes.Select(ProductTypeListItemDto.FromDomain).ToList();
    }
}
