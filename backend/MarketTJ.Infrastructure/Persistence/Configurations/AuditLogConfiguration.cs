using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).IsRequired();
        builder.Property(x => x.EntityType).IsRequired();
        builder.Property(x => x.Details).IsRequired();

        // EntityType+EntityId — полиморфная ссылка (раздел 8.19), не строгий FK.
        builder.HasOne(x => x.Admin)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
