using MediatR;

namespace Orders.Application.Customers.Commands.DeleteCustomer;

public sealed record DeleteCustomerCommand(Guid Id) : IRequest;
