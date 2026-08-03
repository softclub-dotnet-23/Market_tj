using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.WalletDto;

// Намеренно НЕТ CardNumber (полного) и Cvv — см. Wallet.cs: CVV write-only и
// никогда не покидает сервер ни в одном ответе API, полный номер карты
// показывать в UI незачем (только маска CardNumberLast4).
public class GetWalletDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CardHolderFirstName { get; set; } = null!;
    public string CardHolderLastName { get; set; } = null!;
    public CardType CardType { get; set; }
    public string CardNumberLast4 { get; set; } = null!;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string BankName { get; set; } = null!;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
