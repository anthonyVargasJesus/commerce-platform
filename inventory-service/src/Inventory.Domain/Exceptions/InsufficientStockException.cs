namespace Inventory.Domain.Exceptions;

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(Guid productId, int requested, int available)
        : base($"Product '{productId}' has insufficient stock. Requested: {requested}, available: {available}.")
    {
        ProductId = productId;
        Requested = requested;
        Available = available;
    }

    public Guid ProductId { get; }

    public int Requested { get; }

    public int Available { get; }
}
