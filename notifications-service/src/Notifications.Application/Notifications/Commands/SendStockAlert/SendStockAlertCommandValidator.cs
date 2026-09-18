using FluentValidation;

namespace Notifications.Application.Notifications.Commands.SendStockAlert;

public sealed class SendStockAlertCommandValidator : AbstractValidator<SendStockAlertCommand>
{
    public SendStockAlertCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.QuantityOnHand).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}
