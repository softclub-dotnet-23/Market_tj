using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class FarmerDocumentConfiguration : IEntityTypeConfiguration<FarmerDocument>
{
    public void Configure(EntityTypeBuilder<FarmerDocument> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileUrl).IsRequired();

        // Документ — деталь профиля фермера: Cascade.
        builder.HasOne(x => x.FarmerProfile)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.FarmerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
