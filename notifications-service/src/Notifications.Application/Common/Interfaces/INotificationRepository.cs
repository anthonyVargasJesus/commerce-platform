using Notifications.Domain.Notifications;

namespace Notifications.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        Guid? orderId,
        Guid? productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Notification notification);
}
