using Orders.Domain.Orders;

namespace Orders.Application.Common.Security;

public interface IOrderAccessPolicy
{
    Task<OrderScope> GetScopeAsync(CancellationToken cancellationToken);

    // Throws NotFoundException (not Forbidden) for an order the user may not see, so its existence is not revealed.
    Task EnsureCanAccessAsync(Order order, CancellationToken cancellationToken);
}
