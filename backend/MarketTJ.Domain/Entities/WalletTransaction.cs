using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class WalletTransaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public WalletTransactionType Type { get; set; }

    // Знак Amount: TopUp/Refund/FarmerCredit положительны (пополняют),
    // Purchase отрицателен (списывает) — вместе с BalanceAfter это полный
    // аудит-лог движений средств, а не просто текущий баланс кошелька.
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public int? RelatedOrderId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Wallet Wallet { get; set; } = null!;
    public Order? RelatedOrder { get; set; }
}
