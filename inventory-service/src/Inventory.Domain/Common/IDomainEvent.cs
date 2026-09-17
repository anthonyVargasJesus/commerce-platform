namespace Inventory.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
