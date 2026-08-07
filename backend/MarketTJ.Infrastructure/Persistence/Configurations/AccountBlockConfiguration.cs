using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class AccountBlockConfiguration : IEntityTypeConfiguration<AccountBlock>
{
    public void Configure(EntityTypeBuilder<AccountBlock> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).IsRequired().HasMaxLength(20);
        builder.Property(x => x.BlockType).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Reason).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.BlockedUntil });
    }
}
