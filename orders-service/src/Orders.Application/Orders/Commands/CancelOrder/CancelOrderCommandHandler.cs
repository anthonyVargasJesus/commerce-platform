using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Common.Security;
using Orders.Application.Orders.Dtos;
using Orders.Domain.Orders;
using MediatR;

namespace Orders.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IOrderRepository repository,
    IOrderAccessPolicy accessPolicy,
    IInventoryServiceClient inventoryClient,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        await accessPolicy.EnsureCanAccessAsync(order, cancellationToken);

        var wasConfirmed = order.Status == OrderStatus.Confirmed;

        order.Cancel();

        if (wasConfirmed)
        {
            var attemptId = Guid.NewGuid();

            // Compensating call: stock was decremented at ConfirmOrder time, so it must be
            // given back now. Best-effort synchronous call — no saga/outbox exists yet.
            foreach (var item in order.Items)
            {
                await inventoryClient.AdjustStockAsync(item.ProductId, item.Quantity, $"{attemptId}:{item.ProductId}:restock", cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderDto.FromDomain(order);
    }
}
