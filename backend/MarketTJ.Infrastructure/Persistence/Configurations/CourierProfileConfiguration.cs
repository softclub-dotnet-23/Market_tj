using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class CourierProfileConfiguration : IEntityTypeConfiguration<CourierProfile>
{
    public void Configure(EntityTypeBuilder<CourierProfile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransportType).IsRequired();
        builder.Property(x => x.VehicleNumber).IsRequired();
        builder.Property(x => x.Region).IsRequired();
        builder.Property(x => x.District).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();

        // User 1 — 1 CourierProfile: профиль неотделим от пользователя.
        builder.HasOne(x => x.User)
            .WithOne(x => x.CourierProfile)
            .HasForeignKey<CourierProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
