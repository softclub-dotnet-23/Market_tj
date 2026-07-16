using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).IsRequired();
        builder.Property(x => x.DeliveryAddress).IsRequired();
        builder.Property(x => x.Region).IsRequired();
        builder.Property(x => x.District).IsRequired();

        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.DeliveryPrice).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(x => x.OrderNumber).IsUnique();

        // User 1 — many Order (как Customer / как Farmer) — два независимых FK
        // на User, поэтому у каждой стороны своя навигация.
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.OrdersAsCustomer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Farmer)
            .WithMany(x => x.OrdersAsFarmer)
            .HasForeignKey(x => x.FarmerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order 1 — 0..1 Delivery / Review — конфигурируются со стороны
        // Delivery/Review (там связь обязательная), здесь ничего дополнительно
        // настраивать не нужно: EF свяжет по HasOne(x => x.Order) с той стороны.

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
