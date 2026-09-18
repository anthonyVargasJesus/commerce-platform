using Orders.Domain.Orders;

namespace Orders.Application.Orders.Queries.GetOrdersList;

public sealed record OrderListItemDto(Guid Id, Guid CustomerId, OrderStatus Status, decimal TotalAmount, int ItemCount, DateTimeOffset CreatedAt)
{
    public static OrderListItemDto FromDomain(Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status,
        order.TotalAmount,
        order.Items.Count,
        order.CreatedAt);
}
