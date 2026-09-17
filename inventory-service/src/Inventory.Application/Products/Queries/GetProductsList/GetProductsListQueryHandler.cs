using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.Products.Dtos;
using MediatR;

namespace Inventory.Application.Products.Queries.GetProductsList;

public sealed class GetProductsListQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductsListQuery, PaginatedList<ProductDto>>
{
    public async Task<PaginatedList<ProductDto>> Handle(GetProductsListQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(ProductDto.FromDomain).ToList();

        return new PaginatedList<ProductDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
