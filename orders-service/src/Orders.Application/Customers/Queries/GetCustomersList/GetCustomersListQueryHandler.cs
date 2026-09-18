using Orders.Application.Common.Interfaces;
using MediatR;

namespace Orders.Application.Customers.Queries.GetCustomersList;

public sealed class GetCustomersListQueryHandler(ICustomerRepository repository)
    : IRequestHandler<GetCustomersListQuery, IReadOnlyList<CustomerListItemDto>>
{
    public async Task<IReadOnlyList<CustomerListItemDto>> Handle(GetCustomersListQuery request, CancellationToken cancellationToken)
    {
        var customers = await repository.GetAllAsync(cancellationToken);

        return customers.Select(CustomerListItemDto.FromDomain).ToList();
    }
}
