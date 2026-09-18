using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.OrderId).HasColumnName("order_id").IsRequired();

        builder.Property(n => n.CustomerId).HasColumnName("customer_id").IsRequired();

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasColumnName("message")
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(n => n.OrderId);
        builder.HasIndex(n => n.CustomerId);

        builder.Property(n => n.CreatedAt).HasColumnName("created_at");

        builder.Property(n => n.CreatedBy).HasColumnName("created_by").HasMaxLength(100);

        builder.Property(n => n.LastModifiedAt).HasColumnName("last_modified_at");

        builder.Property(n => n.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(100);

        builder.Ignore(n => n.DomainEvents);
    }
}
