using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Notifications.Application.Common.Interfaces;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Notifications;

// Sends the notification as a plain-text email. In development the SMTP server is Mailpit, which
// captures every message instead of delivering it (web UI on port 8025).
public sealed class SmtpNotificationSender(SmtpOptions options, ILogger<SmtpNotificationSender> logger) : INotificationSender
{
    public async Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        var recipient = notification.Recipient ?? options.OperationsEmail;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(options.From));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = SubjectFor(notification.Type);
        message.Body = new TextPart("plain") { Text = notification.Message };

        using var client = new SmtpClient();
        await client.ConnectAsync(options.Host, options.Port, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Email '{Subject}' sent to {Recipient} ({Type})", message.Subject, recipient, notification.Type);
    }

    private static string SubjectFor(NotificationType type) => type switch
    {
        NotificationType.OrderCreated => "We received your order",
        NotificationType.OrderConfirmed => "Your order is confirmed",
        NotificationType.OrderShipped => "Your order has shipped",
        NotificationType.OrderDelivered => "Your order was delivered",
        NotificationType.OrderCancelled => "Your order was cancelled",
        NotificationType.LowStock => "Low stock alert",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown notification type."),
    };
}
