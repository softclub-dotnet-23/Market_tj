using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(x => x.Id);

        // Раздел 10.6: по одному заказу можно оставить только один отзыв.
        builder.HasIndex(x => x.OrderId).IsUnique();

        // Order 1 — 0..1 Review: HasOne/WithOne со стороны Review
        // (обязательная связь), Order.Review — необязательная навигация.
        builder.HasOne(x => x.Order)
            .WithOne(x => x.Review)
            .HasForeignKey<Review>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // User 1 — many Review (как Customer / как Farmer) — два независимых
        // FK на User (обновлено: раньше ссылались на FarmerProfile/CustomerProfile).
        builder.HasOne(x => x.Farmer)
            .WithMany(x => x.ReviewsAsFarmer)
            .HasForeignKey(x => x.FarmerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.ReviewsAsCustomer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
