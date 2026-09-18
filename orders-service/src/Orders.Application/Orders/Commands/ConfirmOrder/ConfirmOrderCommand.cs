using MediatR;
using Orders.Application.Orders.Dtos;

namespace Orders.Application.Orders.Commands.ConfirmOrder;

public sealed record ConfirmOrderCommand(Guid OrderId) : IRequest<OrderDto>;
