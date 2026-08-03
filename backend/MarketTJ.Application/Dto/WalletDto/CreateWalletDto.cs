namespace MarketTJ.Application.Dto.WalletDto;

// CardType сознательно отсутствует — определяется сервером по номеру карты
// (см. WalletValidator.DetectCardType), пользователь его не выбирает.
public class CreateWalletDto
{
    public string CardHolderFirstName { get; set; } = null!;
    public string CardHolderLastName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public string Cvv { get; set; } = null!;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string BankName { get; set; } = null!;
}
