using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Domain.Orders;

namespace Orders.Application.Common.Security;

public sealed class OrderAccessPolicy(ICurrentUser currentUser, ICustomerRepository customerRepository) : IOrderAccessPolicy
{
    public async Task<OrderScope> GetScopeAsync(CancellationToken cancellationToken)
    {
        if (currentUser.CanAccessAllOrders)
        {
            return OrderScope.Unrestricted;
        }

        if (string.IsNullOrWhiteSpace(currentUser.Email))
        {
            return OrderScope.Nothing;
        }

        var customer = await customerRepository.GetByEmailAsync(currentUser.Email, cancellationToken);

        return customer is null ? OrderScope.Nothing : OrderScope.OnlyCustomer(customer.Id);
    }

    public async Task EnsureCanAccessAsync(Order order, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(cancellationToken);

        if (!scope.Allows(order.CustomerId))
        {
            throw new NotFoundException(nameof(Order), order.Id);
        }
    }
}
