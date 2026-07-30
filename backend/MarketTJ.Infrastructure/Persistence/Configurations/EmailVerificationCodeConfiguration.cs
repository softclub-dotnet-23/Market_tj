using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class EmailVerificationCodeConfiguration : IEntityTypeConfiguration<EmailVerificationCode>
{
    public void Configure(EntityTypeBuilder<EmailVerificationCode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.CodeHash).IsRequired();
        builder.HasIndex(x => x.Email);
    }
}
