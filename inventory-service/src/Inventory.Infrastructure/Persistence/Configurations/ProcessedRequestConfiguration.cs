using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class ProcessedRequestConfiguration : IEntityTypeConfiguration<ProcessedRequest>
{
    public void Configure(EntityTypeBuilder<ProcessedRequest> builder)
    {
        builder.ToTable("processed_requests");

        builder.HasKey(r => r.Key);

        builder.Property(r => r.Key).HasColumnName("key").HasMaxLength(200);

        builder.Property(r => r.ProcessedAt).HasColumnName("processed_at");
    }
}
