using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerType).IsRequired();
        builder.Property(x => x.Region).IsRequired();
        builder.Property(x => x.District).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();

        // User 1 — 1 CustomerProfile: профиль неотделим от пользователя.
        builder.HasOne(x => x.User)
            .WithOne(x => x.CustomerProfile)
            .HasForeignKey<CustomerProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
