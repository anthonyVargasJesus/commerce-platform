using FluentValidation;

namespace Inventory.Application.Products.Queries.GetProductsList;

public sealed class GetProductsListQueryValidator : AbstractValidator<GetProductsListQuery>
{
    public GetProductsListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
