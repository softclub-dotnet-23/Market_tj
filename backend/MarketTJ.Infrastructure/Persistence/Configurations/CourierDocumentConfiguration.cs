using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class CourierDocumentConfiguration : IEntityTypeConfiguration<CourierDocument>
{
    public void Configure(EntityTypeBuilder<CourierDocument> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileUrl).IsRequired();

        builder.HasOne(x => x.CourierProfile)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.CourierProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
