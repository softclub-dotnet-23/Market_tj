using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class FarmerProfileConfiguration : IEntityTypeConfiguration<FarmerProfile>
{
    public void Configure(EntityTypeBuilder<FarmerProfile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FarmName).IsRequired();
        builder.Property(x => x.Region).IsRequired();
        builder.Property(x => x.District).IsRequired();
        builder.Property(x => x.Village).IsRequired();
        builder.Property(x => x.Address).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();

        // User 1 — 1 FarmerProfile: профиль неотделим от пользователя.
        builder.HasOne(x => x.User)
            .WithOne(x => x.FarmerProfile)
            .HasForeignKey<FarmerProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // VerifiedByAdminId nullable: если админ удалён — не терять сам профиль,
        // просто обнулить, кто подтвердил (раздел 10.1: подтверждает только Admin).
        builder.HasOne(x => x.VerifiedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.VerifiedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
