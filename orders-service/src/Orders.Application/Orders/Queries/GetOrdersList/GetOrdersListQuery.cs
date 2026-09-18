using MediatR;
using Orders.Application.Common.Models;

namespace Orders.Application.Orders.Queries.GetOrdersList;

public sealed record GetOrdersListQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<OrderListItemDto>>;
