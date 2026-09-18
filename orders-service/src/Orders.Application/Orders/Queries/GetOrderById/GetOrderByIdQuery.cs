using MediatR;
using Orders.Application.Orders.Dtos;

namespace Orders.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;
