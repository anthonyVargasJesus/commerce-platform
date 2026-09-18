using Orders.Domain.Customers;

namespace Orders.Application.Customers.Commands.CreateCustomer;

public sealed record CreatedCustomerDto(Guid Id, string Name, string Email, string? Phone)
{
    public static CreatedCustomerDto FromDomain(Customer customer) => new(customer.Id, customer.Name, customer.Email, customer.Phone);
}
