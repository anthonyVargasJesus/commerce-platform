using Inventory.Domain.Products;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Messaging;

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
        var products = context.ChangeTracker.Entries<Product>()
            .Select(entry => entry.Entity)
            .Where(product => product.DomainEvents.Count > 0)
            .ToList();

        if (products.Count == 0)
        {
            return;
        }

        var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

        foreach (var product in products)
        {
            foreach (var domainEvent in product.DomainEvents)
            {
                await publishEndpoint.Publish(ProductIntegrationEventMapper.Map(product, domainEvent), cancellationToken);
            }

            product.ClearDomainEvents();
        }
    }
}
