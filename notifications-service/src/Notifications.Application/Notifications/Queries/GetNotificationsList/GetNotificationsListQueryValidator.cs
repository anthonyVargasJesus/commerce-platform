using FluentValidation;

namespace Notifications.Application.Notifications.Queries.GetNotificationsList;

public sealed class GetNotificationsListQueryValidator : AbstractValidator<GetNotificationsListQuery>
{
    public GetNotificationsListQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
