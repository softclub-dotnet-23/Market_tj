using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class DeliverySlotConfiguration : IEntityTypeConfiguration<DeliverySlot>
{
    public void Configure(EntityTypeBuilder<DeliverySlot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TimeFrom).IsRequired();
        builder.Property(x => x.TimeTo).IsRequired();

        // Раздел 8.29: один слот на заказ.
        builder.HasIndex(x => x.OrderId).IsUnique();

        builder.HasOne(x => x.Order)
            .WithOne(x => x.DeliverySlot)
            .HasForeignKey<DeliverySlot>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
