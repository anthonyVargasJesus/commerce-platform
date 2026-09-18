namespace Commerce.Contracts.Orders;

// Message contracts are matched by MassTransit on full type name (namespace + name), so every
// service that consumes these events declares its own copy under the same namespace.
public sealed record OrderCreated(Guid OrderId, Guid CustomerId, decimal TotalAmount, DateTimeOffset OccurredOn);

public sealed record OrderConfirmed(Guid OrderId, Guid CustomerId, decimal TotalAmount, DateTimeOffset OccurredOn);

public sealed record OrderShipped(Guid OrderId, Guid CustomerId, decimal TotalAmount, DateTimeOffset OccurredOn);

public sealed record OrderDelivered(Guid OrderId, Guid CustomerId, decimal TotalAmount, DateTimeOffset OccurredOn);

public sealed record OrderCancelled(Guid OrderId, Guid CustomerId, decimal TotalAmount, DateTimeOffset OccurredOn);
