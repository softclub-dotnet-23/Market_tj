using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);

        builder.HasOne(x => x.Wallet)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Конфигурируется со стороны WalletTransaction — на Order обратной
        // навигационной коллекции нет (та же схема, что у Order.Payments/
        // RefundRequests, см. комментарий в OrderConfiguration).
        builder.HasOne(x => x.RelatedOrder)
            .WithMany()
            .HasForeignKey(x => x.RelatedOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.WalletId);
    }
}
