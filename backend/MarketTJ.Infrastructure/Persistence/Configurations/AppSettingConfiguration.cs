using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key).IsRequired();
        builder.Property(x => x.Value).IsRequired();

        builder.HasIndex(x => x.Key).IsUnique();

        builder.HasOne(x => x.UpdatedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.UpdatedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
