using Orders.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.CreatedAt);

        builder.Property(c => c.CreatedBy).HasMaxLength(100);

        builder.Property(c => c.LastModifiedAt);

        builder.Property(c => c.LastModifiedBy).HasMaxLength(100);

        builder.Ignore(c => c.DomainEvents);
    }
}
