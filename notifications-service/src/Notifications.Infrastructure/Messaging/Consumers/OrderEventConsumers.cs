using Commerce.Contracts.Orders;
using MassTransit;
using MediatR;
using Notifications.Application.Notifications.Commands.SendNotification;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Messaging.Consumers;

public sealed class OrderCreatedConsumer(ISender sender) : IConsumer<OrderCreated>
{
    public Task Consume(ConsumeContext<OrderCreated> context) =>
        sender.Send(new SendNotificationCommand(context.Message.OrderId, context.Message.CustomerId, NotificationType.OrderCreated, context.Message.TotalAmount), context.CancellationToken);
}

public sealed class OrderConfirmedConsumer(ISender sender) : IConsumer<OrderConfirmed>
{
    public Task Consume(ConsumeContext<OrderConfirmed> context) =>
        sender.Send(new SendNotificationCommand(context.Message.OrderId, context.Message.CustomerId, NotificationType.OrderConfirmed, context.Message.TotalAmount), context.CancellationToken);
}

public sealed class OrderShippedConsumer(ISender sender) : IConsumer<OrderShipped>
{
    public Task Consume(ConsumeContext<OrderShipped> context) =>
        sender.Send(new SendNotificationCommand(context.Message.OrderId, context.Message.CustomerId, NotificationType.OrderShipped, context.Message.TotalAmount), context.CancellationToken);
}

public sealed class OrderDeliveredConsumer(ISender sender) : IConsumer<OrderDelivered>
{
    public Task Consume(ConsumeContext<OrderDelivered> context) =>
        sender.Send(new SendNotificationCommand(context.Message.OrderId, context.Message.CustomerId, NotificationType.OrderDelivered, context.Message.TotalAmount), context.CancellationToken);
}

public sealed class OrderCancelledConsumer(ISender sender) : IConsumer<OrderCancelled>
{
    public Task Consume(ConsumeContext<OrderCancelled> context) =>
        sender.Send(new SendNotificationCommand(context.Message.OrderId, context.Message.CustomerId, NotificationType.OrderCancelled, context.Message.TotalAmount), context.CancellationToken);
}
