using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class ProductListingConfiguration : IEntityTypeConfiguration<ProductListing>
{
    public void Configure(EntityTypeBuilder<ProductListing> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.QualityGrade).IsRequired();
        builder.Property(x => x.Region).IsRequired();
        builder.Property(x => x.District).IsRequired();
        builder.Property(x => x.Address).IsRequired();

        builder.Property(x => x.RetailPricePerKg).HasPrecision(18, 2);
        builder.Property(x => x.WholesalePricePerKg).HasPrecision(18, 2);
        builder.Property(x => x.WholesaleMinimumQuantity).HasPrecision(18, 3);
        builder.Property(x => x.AvailableQuantity).HasPrecision(18, 3);
        builder.Property(x => x.MinimumOrderQuantity).HasPrecision(18, 3);

        // Product 1 — many ProductListing; Product — справочник (общий тип
        // продукта): Restrict, не Cascade.
        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductListings)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // FarmerProfile 1 — many ProductListing; FarmerProfile — владелец
        // объявления, но не "деталь" в смысле owned-record; удаление фермера
        // не должно молча стирать историю объявлений. Restrict — консервативно.
        builder.HasOne(x => x.FarmerProfile)
            .WithMany(x => x.ProductListings)
            .HasForeignKey(x => x.FarmerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
