using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.ProductTypes;
using Inventory.Domain.Products;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductType> ProductTypes => Set<ProductType>();

    public DbSet<ProcessedRequest> ProcessedRequests => Set<ProcessedRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = utcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
