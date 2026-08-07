using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class AiConversationLogConfiguration : IEntityTypeConfiguration<AiConversationLog>
{
    public void Configure(EntityTypeBuilder<AiConversationLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Question).IsRequired();
        builder.Property(x => x.Response).IsRequired();
        builder.Property(x => x.Intent).IsRequired().HasMaxLength(30);

        // UserId nullable (гость) — не строгий FK ON DELETE CASCADE, чтобы
        // удаление пользователя не роняло журнал диалогов задним числом.
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.WasError);
    }
}
