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

        // Полный номер/CVV — см. подробный комментарий в Wallet.cs (демо-режим,
        // PCI-DSS-исключение только для этого учебного проекта). Значения по
        // умолчанию — для обратной совместимости с уже существующими строками
        // Wallet, созданными до этой миграции (у них не было этих колонок).
        builder.Property(x => x.CardNumber).IsRequired().HasMaxLength(16).HasDefaultValue("0000000000000000");
        builder.Property(x => x.Cvv).IsRequired().HasMaxLength(4).HasDefaultValue("000");
        builder.Property(x => x.ExpiryMonth).IsRequired().HasDefaultValue(12);
        builder.Property(x => x.ExpiryYear).IsRequired().HasDefaultValue(2099);
        builder.Property(x => x.BankName).IsRequired().HasMaxLength(200).HasDefaultValue("—");

        builder.Property(x => x.Balance).HasPrecision(18, 2);

        // Один пользователь — до 5 карт (было: одна карта, unique index).
        // Лимит теперь проверяется на уровне приложения (WalletService.CreateAsync),
        // а не в БД — обычный (не уникальный) индекс остаётся только ради
        // производительности запросов "все карты пользователя".
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
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
