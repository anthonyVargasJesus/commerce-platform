using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Customers;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Messaging;

// Resolves IPublishEndpoint lazily: the bus outbox's endpoint depends on the DbContext, and the
// DbContext depends on this interceptor, so injecting it directly would be a circular dependency.
public sealed class DomainEventsOutboxInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await PublishDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task PublishDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var orders = context.ChangeTracker.Entries<Order>()
            .Select(entry => entry.Entity)
            .Where(order => order.DomainEvents.Count > 0)
            .ToList();

        if (orders.Count == 0)
        {
            return;
        }

        // The events carry the customer's name and email so consumers (e.g. notifications) need not call back.
        var customerIds = orders.Select(order => order.CustomerId).Distinct().ToList();
        var customers = await context.Set<Customer>()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, cancellationToken);

        var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

        foreach (var order in orders)
        {
            foreach (var domainEvent in order.DomainEvents)
            {
                await publishEndpoint.Publish(OrderIntegrationEventMapper.Map(order, customers[order.CustomerId], domainEvent), cancellationToken);
            }

            order.ClearDomainEvents();
        }
    }
}
