using Orders.Application.Common.Interfaces;
using Orders.Application.Common.Models;
using MediatR;

namespace Orders.Application.Orders.Queries.GetOrdersList;

public sealed class GetOrdersListQueryHandler(IOrderRepository repository)
    : IRequestHandler<GetOrdersListQuery, PaginatedList<OrderListItemDto>>
{
    public async Task<PaginatedList<OrderListItemDto>> Handle(GetOrdersListQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(OrderListItemDto.FromDomain).ToList();

        return new PaginatedList<OrderListItemDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
