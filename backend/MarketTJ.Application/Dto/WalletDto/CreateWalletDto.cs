using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.WalletDto;

public class CreateWalletDto
{
    public string CardHolderFirstName { get; set; } = null!;
    public string CardHolderLastName { get; set; } = null!;
    public CardType CardType { get; set; }
}
