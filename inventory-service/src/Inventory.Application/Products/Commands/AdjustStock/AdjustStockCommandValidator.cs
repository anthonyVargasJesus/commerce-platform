using FluentValidation;

namespace Inventory.Application.Products.Commands.AdjustStock;

public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Delta).NotEqual(0);
    }
}
