using MediatR;
using Orders.Application.Orders.Dtos;

namespace Orders.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyList<CreateOrderItemRequest> Items) : IRequest<OrderDto>;

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
