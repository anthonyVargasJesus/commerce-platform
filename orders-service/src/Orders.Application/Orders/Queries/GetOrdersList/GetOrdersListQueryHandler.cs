using Orders.Application.Common.Interfaces;
using Orders.Application.Common.Models;
using Orders.Application.Common.Security;
using MediatR;

namespace Orders.Application.Orders.Queries.GetOrdersList;

public sealed class GetOrdersListQueryHandler(IOrderRepository repository, IOrderAccessPolicy accessPolicy)
    : IRequestHandler<GetOrdersListQuery, PaginatedList<OrderListItemDto>>
{
    public async Task<PaginatedList<OrderListItemDto>> Handle(GetOrdersListQuery request, CancellationToken cancellationToken)
    {
        var scope = await accessPolicy.GetScopeAsync(cancellationToken);

        if (scope.HasNoAccess)
        {
            return new PaginatedList<OrderListItemDto>([], 0, request.PageNumber, request.PageSize);
        }

        var (items, totalCount) = await repository.GetPagedAsync(scope.CustomerId, request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(OrderListItemDto.FromDomain).ToList();

        return new PaginatedList<OrderListItemDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
