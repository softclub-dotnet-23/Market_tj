namespace MarketTJ.Application.Dto.WalletDto;

public class ChangeWalletPinDto
{
    public string CurrentPin { get; set; } = null!;
    public string NewPin { get; set; } = null!;
}
