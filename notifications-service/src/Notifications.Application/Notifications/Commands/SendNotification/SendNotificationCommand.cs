using MediatR;
using Notifications.Domain.Notifications;

namespace Notifications.Application.Notifications.Commands.SendNotification;

public sealed record SendNotificationCommand(Guid OrderId, Guid CustomerId, string CustomerName, string CustomerEmail, NotificationType Type, decimal TotalAmount) : IRequest;
