using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Region).IsRequired();
        builder.Property(x => x.District).IsRequired();
        builder.Property(x => x.BasePrice).HasPrecision(18, 2);
        builder.Property(x => x.PricePerKm).HasPrecision(18, 2);
    }
}
