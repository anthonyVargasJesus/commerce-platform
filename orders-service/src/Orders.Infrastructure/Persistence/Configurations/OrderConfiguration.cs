using Orders.Domain.Customers;
using Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.CreatedAt);

        builder.Property(o => o.CreatedBy).HasMaxLength(100);

        builder.Property(o => o.LastModifiedAt);

        builder.Property(o => o.LastModifiedBy).HasMaxLength(100);

        builder.Ignore(o => o.TotalAmount);

        builder.Ignore(o => o.DomainEvents);

        builder.OwnsMany(o => o.Items, item =>
        {
            item.ToTable("OrderItems");
            item.WithOwner().HasForeignKey("OrderId");
            item.HasKey(i => i.Id);

            item.Property(i => i.ProductId).IsRequired();
            item.Property(i => i.Sku).HasMaxLength(32).IsRequired();
            item.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            item.Property(i => i.UnitPrice).HasPrecision(18, 2);
            item.Property(i => i.Quantity).IsRequired();

            item.Ignore(i => i.LineTotal);
        });

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
