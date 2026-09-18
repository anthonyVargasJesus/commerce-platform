using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Common.Interfaces;
using Notifications.Domain.Common;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Persistence;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);

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
