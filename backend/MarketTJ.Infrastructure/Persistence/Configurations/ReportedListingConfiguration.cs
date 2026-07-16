using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class ReportedListingConfiguration : IEntityTypeConfiguration<ReportedListing>
{
    public void Configure(EntityTypeBuilder<ReportedListing> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ProductListing)
            .WithMany(x => x.Reports)
            .HasForeignKey(x => x.ProductListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReportedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
