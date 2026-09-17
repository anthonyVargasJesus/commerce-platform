using Inventory.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");

        builder.Property(c => c.CreatedBy).HasColumnName("created_by").HasMaxLength(100);

        builder.Property(c => c.LastModifiedAt).HasColumnName("last_modified_at");

        builder.Property(c => c.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(100);

        builder.Ignore(c => c.DomainEvents);
    }
}
