using Orders.Domain.Customers;

namespace Orders.Application.Customers.Queries.GetCustomersList;

public sealed record CustomerListItemDto(Guid Id, string Name, string Email, string? Phone)
{
    public static CustomerListItemDto FromDomain(Customer customer) => new(customer.Id, customer.Name, customer.Email, customer.Phone);
}
