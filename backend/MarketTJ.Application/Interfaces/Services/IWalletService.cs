using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IWalletService
{
    Task<Result<GetWalletDto?>> GetMyWalletAsync();
    Task<Result<GetWalletDto>> CreateAsync(CreateWalletDto dto);
    Task<Result<GetWalletDto>> TopUpAsync(TopUpWalletDto dto);
    Task<Result<IEnumerable<GetWalletTransactionDto>>> GetMyTransactionsAsync();
    Task<Result<GetFarmerPaymentCardDto?>> GetFarmerPaymentCardAsync(int farmerUserId);

    // Вызываются из OrderService (не из контроллера, нет прямого HTTP-пути) —
    // списание при создании заказа, начисление фермеру при завершении и
    // возврат при отклонении/отмене. userId передаётся явно вызывающим
    // сервисом (уже проверенным на владение заказом), а не берётся из
    // currentUser — это межсервисные системные операции, а не действия
    // текущего HTTP-пользователя над собственным кошельком.
    Task<Result<string>> DebitForOrderAsync(int customerUserId, decimal amount, int orderId);
    Task<Result<string>> CreditFarmerForOrderAsync(int farmerUserId, decimal orderSubtotal, int orderId);
    Task<Result<string>> RefundForOrderAsync(int customerUserId, decimal amount, int orderId);
}
