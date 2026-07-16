using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductName).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.TotalPrice).HasPrecision(18, 2);

        // Order 1 — many OrderItem; деталь заказа, вне заказа смысла не
        // имеет: Cascade.
        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductListing может быть скрыт/изменён позже, но OrderItem уже
        // хранит собственную копию названия/цены (раздел 8.11) — поэтому
        // ссылка на ProductListing не обязана каскадно удаляться вместе с ним;
        // Restrict — консервативно, не даём удалить Listing, если по нему есть
        // история заказов.
        builder.HasOne(x => x.ProductListing)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ProductListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
