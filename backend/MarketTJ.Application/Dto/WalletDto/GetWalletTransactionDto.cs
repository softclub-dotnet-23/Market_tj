using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.WalletDto;

public class GetWalletTransactionDto
{
    public int Id { get; set; }
    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public int? RelatedOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
