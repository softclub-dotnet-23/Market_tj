using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class FarmerStaffMemberConfiguration : IEntityTypeConfiguration<FarmerStaffMember>
{
    public void Configure(EntityTypeBuilder<FarmerStaffMember> builder)
    {
        builder.HasKey(x => x.Id);

        // Раздел 8.26: один логин = один сотрудник.
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.FarmerProfile)
            .WithMany(x => x.StaffMembers)
            .HasForeignKey(x => x.FarmerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithOne(x => x.FarmerStaffMember)
            .HasForeignKey<FarmerStaffMember>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
