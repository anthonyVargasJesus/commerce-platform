using Orders.Application.Common.Interfaces;
using Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Orders.Infrastructure.Persistence.Repositories;

public class OrderRepository(OrdersDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Orders.Include(o => o.Items).OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        dbContext.Orders.AnyAsync(o => o.CustomerId == customerId, cancellationToken);

    public void Add(Order order) => dbContext.Orders.Add(order);
}
