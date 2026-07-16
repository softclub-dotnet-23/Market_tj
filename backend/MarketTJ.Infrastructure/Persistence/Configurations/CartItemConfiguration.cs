using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(18, 3);

        // Раздел 8.9: один и тот же товар не должен повторяться в корзине.
        builder.HasIndex(x => new { x.CustomerId, x.ProductListingId }).IsUnique();

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.CartItems)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductListing)
            .WithMany(x => x.CartItems)
            .HasForeignKey(x => x.ProductListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
