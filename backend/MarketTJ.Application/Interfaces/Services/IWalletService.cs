using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IWalletService
{
    Task<Result<IEnumerable<GetWalletDto>>> GetMyWalletsAsync();
    Task<Result<GetWalletDto>> CreateAsync(CreateWalletDto dto);
    Task<Result<GetWalletDto>> TopUpAsync(int walletId, TopUpWalletDto dto);
    Task<Result<IEnumerable<GetWalletTransactionDto>>> GetTransactionsAsync(int walletId);
    Task<Result<GetFarmerPaymentCardDto?>> GetFarmerPaymentCardAsync(int farmerUserId);

    // Вызываются из OrderService (не из контроллера, нет прямого HTTP-пути) —
    // списание при создании заказа, начисление фермеру при завершении и
    // возврат при отклонении/отмене. userId передаётся явно вызывающим
    // сервисом (уже проверенным на владение заказом), а не берётся из
    // currentUser — это межсервисные системные операции, а не действия
    // текущего HTTP-пользователя над собственным кошельком.
    //
    // DebitForOrderAsync принимает конкретный walletId (карту, выбранную
    // покупателем при оформлении заказа) — и внутри себя проверяет, что
    // wallet.UserId == customerUserId, иначе покупатель мог бы передать
    // чужой walletId и списать деньги с чужой карты (IDOR).
    Task<Result<string>> DebitForOrderAsync(int customerUserId, int walletId, decimal amount, int orderId);
    Task<Result<string>> CreditFarmerForOrderAsync(int farmerUserId, decimal orderSubtotal, int orderId);

    // Возврат не принимает customerUserId/amount/walletId явно — карта и
    // сумма определяются по исходной Purchase-транзакции этого заказа (см.
    // WalletService.RefundForOrderAsync), поэтому деньги всегда возвращаются
    // на ту же карту, с которой были списаны. Если списания не было (заказ
    // оплачен наличными) — no-op.
    Task<Result<string>> RefundForOrderAsync(int orderId);
}
