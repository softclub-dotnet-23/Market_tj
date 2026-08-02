using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CardHolderFirstName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CardHolderLastName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CardNumberLast4).IsRequired().HasMaxLength(4);
        builder.Property(x => x.Balance).HasPrecision(18, 2);

        // Один пользователь — одна карта: unique constraint на уровне БД, а
        // не только проверка в сервисе — защищает от гонки, если один и тот
        // же пользователь параллельно отправит два запроса на создание карты.
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Wallet>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // CHECK на уровне БД — последний рубеж защиты от отрицательного
        // баланса, даже если бы прикладная логика где-то дала сбой.
        builder.ToTable(t => t.HasCheckConstraint("CK_Wallets_Balance_NonNegative", "\"Balance\" >= 0"));

        // Optimistic-concurrency токен — обычная колонка-счётчик (см.
        // Wallet.Version), а не Postgres-специфичный xmin: провайдер Npgsql
        // в используемой версии не предоставляет готового хелпера для
        // xmin-конкуренции, а app-managed Version переносим между любыми
        // провайдерами и проще тестировать. EF Core включает исходное
        // значение в WHERE UPDATE-запроса и бросает DbUpdateConcurrencyException,
        // если строку успел изменить кто-то ещё между чтением и записью
        // (см. WalletRepository.TryApplyTransactionAsync,
        // WalletService.AdjustBalanceAsync — защита от гонки при двух
        // одновременных списаниях с одного баланса).
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
