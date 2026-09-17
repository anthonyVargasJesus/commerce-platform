using FluentValidation;

namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.InitialQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0);
    }
}
