using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CardHolderFirstName { get; set; } = null!;
    public string CardHolderLastName { get; set; } = null!;

    // Тип карты не вводится пользователем — определяется сервером по первым
    // цифрам CardNumber (см. WalletValidator.DetectCardType), как в реальных
    // платёжных системах. Дополнительно служит проверкой, что номер карты
    // соответствует заявленному формату.
    public CardType CardType { get; set; }

    // ВАЖНО — ПОЛНЫЙ НОМЕР КАРТЫ И CVV ХРАНЯТСЯ В ОТКРЫТОМ ВИДЕ.
    // Это учебный демо-проект: карта полностью виртуальная (внутренний
    // баланс платформы, не подключена к реальному платёжному шлюзу/банку),
    // деньги не настоящие — это симуляция для учебных целей. Полный PAN и
    // CVV нужны здесь только для реалистичности формы (валидация по
    // алгоритму Луна, автоопределение типа карты, маскирование при показе).
    // В РЕАЛЬНОЙ ПРОДАКШН-СИСТЕМЕ С НАСТОЯЩИМИ ДЕНЬГАМИ ТАК ДЕЛАТЬ НЕЛЬЗЯ —
    // это прямое нарушение PCI-DSS (Payment Card Industry Data Security
    // Standard), который запрещает хранить полный номер карты без сильного
    // шифрования/токенизации на сертифицированной инфраструктуре и вообще
    // запрещает хранить CVV после авторизации платежа ни в каком виде. В
    // реальном продакшене вместо этих полей должен использоваться токен от
    // сертифицированного PCI-DSS платёжного провайдера (Stripe, YooKassa и
    // т.п.), а полный номер/CVV никогда не должны попадать на сервер платформы.
    public string CardNumber { get; set; } = null!;

    // Write-only с точки зрения API — присутствует в CreateWalletDto (вход),
    // но НИКОГДА не включается ни в один Get*Dto (см. WalletService.ToGetDto)
    // и, соответственно, никогда не возвращается ни при создании карты, ни
    // при последующих запросах.
    public string Cvv { get; set; } = null!;

    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string BankName { get; set; } = null!;

    // Последние 4 цифры выводятся из CardNumber при создании карты — не
    // хранить отдельно вычисляемое значение было бы чище, но так UI-запросы
    // (список карт, публичная карта фермера) не должны трогать полный номер
    // вообще, даже читать его из БД лишний раз.
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

    // User 1 — many Wallet (до 5 карт на пользователя, лимит проверяется в
    // WalletService.CreateAsync на уровне приложения, а не unique index —
    // см. обоснование в WalletConfiguration).
    public User User { get; set; } = null!;

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
