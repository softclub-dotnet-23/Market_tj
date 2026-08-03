namespace MarketTJ.Application.Dto.WalletDto;

// Первичная установка PIN — требует текущий пароль пользователя, чтобы
// подтвердить, что это действительно владелец аккаунта (а не, например,
// кто-то, кто на минуту получил доступ к уже разблокированной сессии).
public class SetWalletPinDto
{
    public string Pin { get; set; } = null!;
    public string Password { get; set; } = null!;
}
