using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

// Отдельный сервис от IWalletService намеренно — PIN защищает вход в РАЗДЕЛ
// "Кошелёк" на клиенте (не подменяет JWT-авторизацию самих эндпоинтов
// кошелька, см. WalletController) и живёт на User, а не на Wallet — другая
// сущность, другой репозиторий (IUserRepository, не IWalletRepository).
public interface IWalletPinService
{
    Task<Result<WalletPinStatusDto>> GetStatusAsync();
    Task<Result<string>> SetPinAsync(SetWalletPinDto dto);
    Task<Result<string>> VerifyPinAsync(VerifyWalletPinDto dto);
    Task<Result<string>> ChangePinAsync(ChangeWalletPinDto dto);
}
