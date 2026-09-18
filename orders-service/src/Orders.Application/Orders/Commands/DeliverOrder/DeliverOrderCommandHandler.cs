using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Orders.Dtos;
using Orders.Domain.Orders;
using MediatR;

namespace Orders.Application.Orders.Commands.DeliverOrder;

public sealed class DeliverOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeliverOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.Deliver();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderDto.FromDomain(order);
    }
}
