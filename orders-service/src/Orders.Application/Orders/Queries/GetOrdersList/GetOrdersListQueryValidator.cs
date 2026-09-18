using FluentValidation;

namespace Orders.Application.Orders.Queries.GetOrdersList;

public sealed class GetOrdersListQueryValidator : AbstractValidator<GetOrdersListQuery>
{
    public GetOrdersListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
