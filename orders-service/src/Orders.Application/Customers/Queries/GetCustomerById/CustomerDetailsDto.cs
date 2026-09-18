using Orders.Domain.Customers;

namespace Orders.Application.Customers.Queries.GetCustomerById;

public sealed record CustomerDetailsDto(Guid Id, string Name, string Email, string? Phone, DateTimeOffset CreatedAt)
{
    public static CustomerDetailsDto FromDomain(Customer customer) => new(customer.Id, customer.Name, customer.Email, customer.Phone, customer.CreatedAt);
}
