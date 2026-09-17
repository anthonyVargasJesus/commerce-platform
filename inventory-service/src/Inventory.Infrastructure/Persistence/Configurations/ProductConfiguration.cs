using Inventory.Domain.Categories;
using Inventory.Domain.ProductTypes;
using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value))
            .HasColumnName("sku")
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasFilter("is_active = true");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(p => p.Price)
            .HasColumnName("price")
            .HasPrecision(18, 2);

        builder.Property(p => p.QuantityOnHand).HasColumnName("quantity_on_hand");

        builder.Property(p => p.ReorderLevel).HasColumnName("reorder_level");

        builder.Property(p => p.IsActive).HasColumnName("is_active");

        builder.Property(p => p.CategoryId).HasColumnName("category_id");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.ProductTypeId).HasColumnName("product_type_id");

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(p => p.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at");

        builder.Property(p => p.CreatedBy).HasColumnName("created_by").HasMaxLength(100);

        builder.Property(p => p.LastModifiedAt).HasColumnName("last_modified_at");

        builder.Property(p => p.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(100);

        builder.HasQueryFilter(p => p.IsActive);

        builder.Ignore(p => p.DomainEvents);
    }
}
