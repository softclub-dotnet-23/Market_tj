using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).IsRequired();
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.PhoneNumber).IsRequired();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.Role).IsRequired();

        // Partial-индекс (WHERE "IsDeleted" = false) — иначе email/телефон
        // мягко удалённого пользователя навсегда блокирует регистрацию нового
        // с тем же email/телефоном: GetByEmailAsync/RegisterAsync проверяют
        // "существует ли уже" через query filter (!IsDeleted), видят "свободно",
        // но INSERT падает на констрейнте БД, который про IsDeleted не знает
        // (баг воспроизведён 2026-07-30: удалили покупателя из админки, тут же
        // попытались зарегистрировать того же человека заново — падало с
        // generic "Не удалось зарегистрироваться").
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.PhoneNumber).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
