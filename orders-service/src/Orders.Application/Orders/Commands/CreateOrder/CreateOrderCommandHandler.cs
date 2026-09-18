using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Orders.Dtos;
using Orders.Domain.Orders;
using MediatR;

namespace Orders.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(IOrderRepository repository, IInventoryServiceClient inventoryClient, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var items = new List<OrderItem>();

        foreach (var itemRequest in request.Items)
        {
            var product = await inventoryClient.GetProductAsync(itemRequest.ProductId, cancellationToken)
                ?? throw new NotFoundException("Product", itemRequest.ProductId);

            if (!product.IsActive)
            {
                throw new ConflictException($"Product '{product.Sku}' is not available.");
            }

            items.Add(OrderItem.Create(product.Id, product.Sku, product.Name, product.Price, itemRequest.Quantity));
        }

        var order = Order.Create(request.CustomerId, items);

        repository.Add(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderDto.FromDomain(order);
    }
}
