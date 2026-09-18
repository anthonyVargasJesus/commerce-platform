using MediatR;
using Orders.Application.Orders.Dtos;

namespace Orders.Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest<OrderDto>;
