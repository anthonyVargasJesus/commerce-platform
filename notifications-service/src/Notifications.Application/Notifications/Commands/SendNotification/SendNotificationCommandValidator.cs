using FluentValidation;

namespace Notifications.Application.Notifications.Commands.SendNotification;

public sealed class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty();
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
    }
}
