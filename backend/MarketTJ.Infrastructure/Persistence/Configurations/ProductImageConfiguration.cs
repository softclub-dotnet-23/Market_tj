using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImageUrl).IsRequired();

        // ProductListing 1 — many ProductImage; изображение — деталь
        // объявления, вне объявления смысла не имеет: Cascade при удалении
        // ProductListing (раздел 17: удаление объявления удаляет/помечает
        // удалёнными его изображения).
        builder.HasOne(x => x.ProductListing)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
