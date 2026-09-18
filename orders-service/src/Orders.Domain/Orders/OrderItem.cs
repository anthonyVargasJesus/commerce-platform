using Orders.Domain.Exceptions;

namespace Orders.Domain.Orders;

public sealed class OrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    private OrderItem()
    {
    }

    public static OrderItem Create(Guid productId, string sku, string productName, decimal unitPrice, int quantity)
    {
        if (unitPrice < 0)
        {
            throw new DomainException("Order item unit price cannot be negative.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Order item quantity must be greater than zero.");
        }

        return new OrderItem
        {
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
        };
    }
}
