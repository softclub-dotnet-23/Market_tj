using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PickupAddress).IsRequired();
        builder.Property(x => x.DeliveryAddress).IsRequired();
        builder.Property(x => x.DeliveryPrice).HasPrecision(18, 2);

        // Раздел 8.12: один заказ имеет максимум одну активную доставку.
        builder.HasIndex(x => x.OrderId).IsUnique();

        // Order 1 — 0..1 Delivery: HasOne/WithOne со стороны Delivery
        // (обязательная связь), Order.Delivery — необязательная навигация.
        builder.HasOne(x => x.Order)
            .WithOne(x => x.Delivery)
            .HasForeignKey<Delivery>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // CourierId nullable (доставка может существовать до назначения курьера,
        // DeliveryStatus.Pending). Если профиль курьера удалён — не блокируем
        // удаление и не теряем саму доставку, просто обнуляем CourierId (SetNull).
        builder.HasOne(x => x.Courier)
            .WithMany(x => x.Deliveries)
            .HasForeignKey(x => x.CourierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
