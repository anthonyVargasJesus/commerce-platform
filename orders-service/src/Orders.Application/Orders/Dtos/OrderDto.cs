using Orders.Domain.Orders;

namespace Orders.Application.Orders.Dtos;

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items)
{
    public static OrderDto FromDomain(Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status,
        order.TotalAmount,
        order.CreatedAt,
        order.Items.Select(OrderItemDto.FromDomain).ToList());
}

public sealed record OrderItemDto(Guid ProductId, string Sku, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal)
{
    public static OrderItemDto FromDomain(OrderItem item) => new(
        item.ProductId,
        item.Sku,
        item.ProductName,
        item.UnitPrice,
        item.Quantity,
        item.LineTotal);
}
