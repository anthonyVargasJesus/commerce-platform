using MediatR;
using Orders.Application.Orders.Dtos;

namespace Orders.Application.Orders.Commands.DeliverOrder;

public sealed record DeliverOrderCommand(Guid OrderId) : IRequest<OrderDto>;
