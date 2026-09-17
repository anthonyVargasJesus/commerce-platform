using Inventory.Application.Common.Models;
using Inventory.Application.Products.Dtos;
using MediatR;

namespace Inventory.Application.Products.Queries.GetProductsList;

public sealed record GetProductsListQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<ProductDto>>;
