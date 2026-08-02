using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CardHolderFirstName { get; set; } = null!;
    public string CardHolderLastName { get; set; } = null!;
    public CardType CardType { get; set; }

    // Полный номер карты сознательно НЕ хранится и не генерируется —
    // карта виртуальная (внутренний баланс платформы, а не настоящий
    // платёжный инструмент), полный номер нигде в системе не нужен, только
    // маска для UI ("•••• •••• •••• 1234"). Последние 4 цифры генерируются
    // криптографически случайно при создании карты (см. WalletService) —
    // это исключает весь класс рисков хранения/шифрования/утечки полного PAN.
    public string CardNumberLast4 { get; set; } = null!;

    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Optimistic-concurrency токен (см. WalletConfiguration.IsConcurrencyToken,
    // WalletRepository.TryApplyTransactionAsync) — обычная колонка-счётчик,
    // не связана с Postgres xmin: инкрементируется вручную при каждом
    // изменении баланса. EF Core сравнивает исходное значение в WHERE
    // UPDATE-запроса и бросает DbUpdateConcurrencyException, если строку
    // успел изменить кто-то ещё между чтением и записью — защита от гонки
    // при двух одновременных списаниях с одного и того же кошелька.
    public int Version { get; set; }

    // User 1 — 1 Wallet (один пользователь = одна карта, unique index в
    // WalletConfiguration защищает и на уровне БД, не только в сервисе).
    public User User { get; set; } = null!;

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
