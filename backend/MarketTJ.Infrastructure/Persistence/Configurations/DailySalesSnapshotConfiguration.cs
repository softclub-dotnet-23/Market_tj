using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class DailySalesSnapshotConfiguration : IEntityTypeConfiguration<DailySalesSnapshot>
{
    public void Configure(EntityTypeBuilder<DailySalesSnapshot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalRevenue).HasPrecision(18, 2);
        builder.Property(x => x.TotalCommission).HasPrecision(18, 2);

        // Раздел 8.30: уникально per день.
        builder.HasIndex(x => x.Date).IsUnique();
    }
}
