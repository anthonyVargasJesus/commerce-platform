using Microsoft.Extensions.Logging;
using Notifications.Application.Common.Interfaces;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Notifications;

// Stand-in for a real channel (email/SMS/push). The customer's contact details live in
// orders-service and are not part of the order events yet, so for now "sending" is a log entry.
public sealed class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Notification {Type} for customer {CustomerId} (order {OrderId}): {Message}",
            notification.Type,
            notification.CustomerId,
            notification.OrderId,
            notification.Message);

        return Task.CompletedTask;
    }
}
