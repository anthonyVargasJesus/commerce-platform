namespace Commerce.Contracts.Orders;

// Consumer-side copy of the contracts published by orders-service. MassTransit matches messages by
// full type name (namespace + name), so this must stay identical to the publisher's declaration.
public sealed record OrderCreated(Guid OrderId, Guid CustomerId, string CustomerName, string CustomerEmail, decimal TotalAmount, DateTimeOffset OccurredOn);

public sealed record OrderConfirmed(Guid OrderId, Guid CustomerId, string CustomerName, string CustomerEmail, decimal TotalAmount, DateTimeOffset OccurredOn);

public sealed record OrderShipped(Guid OrderId, Guid CustomerId, string CustomerName, string CustomerEmail, decimal TotalAmount, DateTimeOffset OccurredOn);

public sealed record OrderDelivered(Guid OrderId, Guid CustomerId, string CustomerName, string CustomerEmail, decimal TotalAmount, DateTimeOffset OccurredOn);

public sealed record OrderCancelled(Guid OrderId, Guid CustomerId, string CustomerName, string CustomerEmail, decimal TotalAmount, DateTimeOffset OccurredOn);
