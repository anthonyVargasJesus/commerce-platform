namespace Notifications.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
