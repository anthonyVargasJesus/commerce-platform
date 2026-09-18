using Commerce.Contracts.Inventory;
using MassTransit;
using MediatR;
using Notifications.Application.Notifications.Commands.SendStockAlert;

namespace Notifications.Infrastructure.Messaging.Consumers;

public sealed class ProductLowStockConsumer(ISender sender) : IConsumer<ProductLowStock>
{
    public Task Consume(ConsumeContext<ProductLowStock> context) =>
        sender.Send(
            new SendStockAlertCommand(
                context.Message.ProductId,
                context.Message.Sku,
                context.Message.Name,
                context.Message.QuantityOnHand,
                context.Message.ReorderLevel),
            context.CancellationToken);
}
