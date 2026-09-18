using MediatR;

namespace Orders.Application.Customers.Queries.GetCustomersList;

public sealed record GetCustomersListQuery : IRequest<IReadOnlyList<CustomerListItemDto>>;
