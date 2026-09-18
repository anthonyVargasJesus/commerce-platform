using Orders.Domain.Orders;

namespace Orders.Domain.Exceptions;

public class InvalidOrderStateTransitionException : DomainException
{
    public InvalidOrderStateTransitionException(OrderStatus currentStatus, string attemptedAction)
        : base($"Cannot perform '{attemptedAction}' on an order in status '{currentStatus}'.")
    {
        CurrentStatus = currentStatus;
        AttemptedAction = attemptedAction;
    }

    public OrderStatus CurrentStatus { get; }

    public string AttemptedAction { get; }
}
