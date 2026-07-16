using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.RefundRequests)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProcessedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ProcessedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Раздел 8.21: у одного Order не может быть двух Pending RefundRequest
        // одновременно — уникальный частичный индекс (только среди Pending).
        builder.HasIndex(x => x.OrderId)
            .HasFilter("\"Status\" = 1")
            .IsUnique();
    }
}
