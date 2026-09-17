using Inventory.Domain.ProductTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("product_types");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(pt => pt.Name).IsUnique();

        builder.Property(pt => pt.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(pt => pt.CreatedAt).HasColumnName("created_at");

        builder.Property(pt => pt.CreatedBy).HasColumnName("created_by").HasMaxLength(100);

        builder.Property(pt => pt.LastModifiedAt).HasColumnName("last_modified_at");

        builder.Property(pt => pt.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(100);

        builder.Ignore(pt => pt.DomainEvents);
    }
}
