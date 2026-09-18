using MediatR;

namespace Orders.Application.Customers.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(Guid Id, string Name, string Email, string? Phone) : IRequest;
