using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.WalletDto;

public class GetWalletDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CardHolderFirstName { get; set; } = null!;
    public string CardHolderLastName { get; set; } = null!;
    public CardType CardType { get; set; }
    public string CardNumberLast4 { get; set; } = null!;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
