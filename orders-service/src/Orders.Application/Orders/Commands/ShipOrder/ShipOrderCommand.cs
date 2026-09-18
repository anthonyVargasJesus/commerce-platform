using MediatR;
using Orders.Application.Orders.Dtos;

namespace Orders.Application.Orders.Commands.ShipOrder;

public sealed record ShipOrderCommand(Guid OrderId) : IRequest<OrderDto>;
