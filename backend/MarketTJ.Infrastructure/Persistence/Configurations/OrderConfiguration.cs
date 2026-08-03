using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
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

        // Все заказы, созданные до этой миграции, были оплачены картой (это
        // единственный способ оплаты, существовавший на тот момент) и уже
        // списаны — поэтому дефолты для обратной совместимости: Card + IsPaid=true.
        builder.Property(x => x.PaymentMethod).HasDefaultValue(OrderPaymentMethod.Card);
        builder.Property(x => x.IsPaid).HasDefaultValue(true);

        // WalletId — не строгая FK-навигация (со стороны Order нет свойства
        // Wallet), а просто ссылка на конкретную карту, с которой было
        // списание при оплате Card; SetNull на удаление карты не предусмотрен
        // (карты не удаляются в этой версии), Restrict — чтобы не потерять
        // историческую ссылку молча.
        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // CustomerProfile/FarmerProfile 1 — many Order (раздел 9 TZ1.md — снова
        // через профили, не напрямую через User).
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Farmer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.FarmerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order 1 — 0..1 Delivery / Review / DeliverySlot / Conversation —
        // конфигурируются со стороны дочерней сущности (там связь обязательная).
        // Order 1 — 0..many Payment / RefundRequest — конфигурируются со
        // стороны Payment/RefundRequest.

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
