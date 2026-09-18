using Notifications.Domain.Notifications;

namespace Notifications.Application.Common.Interfaces;

public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken);
}
