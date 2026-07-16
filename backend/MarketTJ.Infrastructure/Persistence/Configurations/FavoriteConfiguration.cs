using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(x => x.Id);

        // Раздел 8.25: уникальность пары Customer+ProductListing.
        builder.HasIndex(x => new { x.CustomerId, x.ProductListingId }).IsUnique();

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Favorites)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProductListing)
            .WithMany(x => x.Favorites)
            .HasForeignKey(x => x.ProductListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
