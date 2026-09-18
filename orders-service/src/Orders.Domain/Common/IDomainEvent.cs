namespace Orders.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
