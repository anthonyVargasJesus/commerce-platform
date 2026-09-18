using Orders.Domain.Orders;

namespace Orders.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    void Add(Order order);
}
