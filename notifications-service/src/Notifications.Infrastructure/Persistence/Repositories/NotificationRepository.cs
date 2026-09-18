using Microsoft.EntityFrameworkCore;
using Notifications.Application.Common.Interfaces;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Persistence.Repositories;

public class NotificationRepository(NotificationsDbContext dbContext) : INotificationRepository
{
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        Guid? orderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications.AsQueryable();

        if (customerId is not null)
        {
            query = query.Where(n => n.CustomerId == customerId);
        }

        if (orderId is not null)
        {
            query = query.Where(n => n.OrderId == orderId);
        }

        var ordered = query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(Notification notification) => dbContext.Notifications.Add(notification);
}
