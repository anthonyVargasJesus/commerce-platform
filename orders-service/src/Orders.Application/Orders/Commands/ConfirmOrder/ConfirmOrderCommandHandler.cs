using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Common.Security;
using Orders.Application.Orders.Dtos;
using Orders.Domain.Orders;
using MediatR;

namespace Orders.Application.Orders.Commands.ConfirmOrder;

public sealed class ConfirmOrderCommandHandler(
    IOrderRepository repository,
    IOrderAccessPolicy accessPolicy,
    IInventoryServiceClient inventoryClient,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        await accessPolicy.EnsureCanAccessAsync(order, cancellationToken);

        await ReserveStockOrThrowAsync(order, cancellationToken);

        order.Confirm();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderDto.FromDomain(order);
    }

    /// <summary>
    /// Decrements stock in inventory-service for every line item. If any item fails
    /// (product missing or insufficient stock), the items already decremented in this
    /// same call are compensated (restocked) before throwing, so a partial failure never
    /// leaves inventory silently short. There is no saga/outbox yet, so this compensation
    /// is a best-effort synchronous call — see CLAUDE.md notes on that limitation.
    /// </summary>
    private async Task ReserveStockOrThrowAsync(Order order, CancellationToken cancellationToken)
    {
        var reserved = new List<OrderItem>();

        foreach (var item in order.Items)
        {
            var result = await inventoryClient.AdjustStockAsync(item.ProductId, -item.Quantity, cancellationToken);

            if (result == InventoryAdjustmentResult.Success)
            {
                reserved.Add(item);
                continue;
            }

            foreach (var toCompensate in reserved)
            {
                await inventoryClient.AdjustStockAsync(toCompensate.ProductId, toCompensate.Quantity, cancellationToken);
            }

            throw result switch
            {
                InventoryAdjustmentResult.InsufficientStock => new ConflictException($"Insufficient stock for product '{item.Sku}'."),
                InventoryAdjustmentResult.ProductNotFound => new NotFoundException("Product", item.ProductId),
                _ => new ConflictException($"Could not reserve stock for product '{item.Sku}'."),
            };
        }
    }
}
