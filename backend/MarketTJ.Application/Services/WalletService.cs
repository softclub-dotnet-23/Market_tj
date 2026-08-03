using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Внутренний кошелёк (виртуальная карта) для Customer/Farmer — платформенная
// валюта внутри Market.tj, не интеграция с настоящим платёжным провайдером.
// До 5 карт на пользователя (лимит проверяется здесь, в CreateAsync — см.
// WalletConfiguration про отказ от unique index). Все изменения баланса
// проходят через единственный путь — AdjustBalanceAsync — и всегда
// сопровождаются строкой в WalletTransaction (полный аудит-лог, не просто
// текущее число).
public class WalletService(
    IWalletRepository walletRepository,
    ICommissionRepository commissionRepository,
    ICurrentUserService currentUser,
    ILogger<WalletService> logger) : IWalletService
{
    // Разумный верхний предел на баланс в целом — защита от переполнения/
    // абсурдных значений, а не только от переполнения per-operation лимита
    // на пополнение (см. WalletValidator.MaxTopUpAmount).
    private const decimal MaxBalance = 1_000_000m;

    // Сколько раз перечитать баланс и повторить запись при конфликте
    // оптимистичной блокировки, прежде чем сдаться — два реальных
    // одновременных списания с одного и того же кошелька случаются редко
    // даже под нагрузкой, этого запаса достаточно.
    private const int MaxConcurrencyRetries = 5;

    public async Task<Result<IEnumerable<GetWalletDto>>> GetMyWalletsAsync()
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<IEnumerable<GetWalletDto>>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            var wallets = await walletRepository.GetAllByUserIdAsync(currentUser.UserId.Value);
            return Result<IEnumerable<GetWalletDto>>.Ok(wallets.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка карт");
            return Result<IEnumerable<GetWalletDto>>.Fail("Не удалось получить список карт", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetWalletDto>> CreateAsync(CreateWalletDto dto)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<GetWalletDto>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            var validation = WalletValidator.ValidateCreate(dto);
            if (validation is not null)
                return Result<GetWalletDto>.Fail(validation.Error!, validation.ErrorType!.Value);

            var cardNumber = dto.CardNumber.Replace(" ", "").Trim();

            // Быстрая проверка лимита до похода в БД на запись. Проверка
            // намеренно только на уровне приложения (не unique index, как
            // раньше для "1 карта") — см. WalletConfiguration и
            // IWalletRepository.TryAddAsync: два одновременных запроса от
            // одного пользователя на 5-й/6-й карте теоретически могут оба
            // пройти эту проверку (TOCTOU) и создать 6 карт — сознательно не
            // устраняем эту редкую гонку более тяжёлой машинерией (БД-триггер
            // и т.п.), т.к. цена ошибки здесь минимальна (лишняя виртуальная
            // карта, не потеря денег).
            var existing = await walletRepository.GetAllByUserIdAsync(currentUser.UserId.Value);
            if (existing.Count >= WalletValidator.MaxCardsPerUser)
                return Result<GetWalletDto>.Fail($"Нельзя иметь больше {WalletValidator.MaxCardsPerUser} карт", ErrorType.Conflict);

            var cardType = WalletValidator.DetectCardType(cardNumber)!.Value;
            var expiryYear = dto.ExpiryYear is >= 0 and <= 99 ? 2000 + dto.ExpiryYear : dto.ExpiryYear;

            var wallet = new Wallet
            {
                UserId = currentUser.UserId.Value,
                CardHolderFirstName = dto.CardHolderFirstName.Trim(),
                CardHolderLastName = dto.CardHolderLastName.Trim(),
                CardType = cardType,
                CardNumber = cardNumber,
                Cvv = dto.Cvv.Trim(),
                ExpiryMonth = dto.ExpiryMonth,
                ExpiryYear = expiryYear,
                BankName = dto.BankName.Trim(),
                CardNumberLast4 = cardNumber[^4..],
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await walletRepository.TryAddAsync(wallet);
            if (!created)
                return Result<GetWalletDto>.Fail("Не удалось создать карту, попробуйте ещё раз", ErrorType.Conflict);

            return Result<GetWalletDto>.Ok(ToGetDto(wallet));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании кошелька");
            return Result<GetWalletDto>.Fail("Не удалось создать карту", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetWalletDto>> TopUpAsync(int walletId, TopUpWalletDto dto)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<GetWalletDto>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            var validation = WalletValidator.ValidateTopUp(dto);
            if (validation is not null)
                return Result<GetWalletDto>.Fail(validation.Error!, validation.ErrorType!.Value);

            // IDOR-guard (нельзя пополнить чужую карту, зная её id) проверяется
            // внутри AdjustBalanceAsync на каждой попытке — так же, как и сам
            // баланс, вместо отдельного предварительного чтения кошелька
            // (лишний round-trip в БД перед циклом retry).
            var adjusted = await AdjustBalanceAsync(walletId, dto.Amount, WalletTransactionType.TopUp, null, expectedOwnerUserId: currentUser.UserId.Value);
            if (!adjusted.IsSuccess)
                return Result<GetWalletDto>.Fail(adjusted.Error!, adjusted.ErrorType!.Value);

            return Result<GetWalletDto>.Ok(ToGetDto(adjusted.Data!));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при пополнении кошелька");
            return Result<GetWalletDto>.Fail("Не удалось пополнить баланс", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<GetWalletTransactionDto>>> GetTransactionsAsync(int walletId)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<IEnumerable<GetWalletTransactionDto>>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            var wallet = await walletRepository.GetByIdAsync(walletId);
            if (wallet is null)
                return Result<IEnumerable<GetWalletTransactionDto>>.Fail("Карта не найдена", ErrorType.NotFound);

            if (wallet.UserId != currentUser.UserId.Value)
                return Result<IEnumerable<GetWalletTransactionDto>>.Fail("Нет доступа к этой карте", ErrorType.Forbidden);

            var transactions = await walletRepository.GetTransactionsAsync(wallet.Id);
            return Result<IEnumerable<GetWalletTransactionDto>>.Ok(
                transactions.OrderByDescending(t => t.CreatedAt).Select(ToTransactionDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении истории операций кошелька");
            return Result<IEnumerable<GetWalletTransactionDto>>.Fail("Не удалось получить историю операций", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetFarmerPaymentCardDto?>> GetFarmerPaymentCardAsync(int farmerUserId)
    {
        try
        {
            // Самая старая карта фермера — намеренное упрощение: отдельного
            // "выбора карты для выплат" в этой версии нет (см. GetFarmerPaymentCardDto).
            var wallet = (await walletRepository.GetAllByUserIdAsync(farmerUserId)).FirstOrDefault();
            return Result<GetFarmerPaymentCardDto?>.Ok(wallet is null
                ? null
                : new GetFarmerPaymentCardDto { CardType = wallet.CardType, CardNumberLast4 = wallet.CardNumberLast4, BankName = wallet.BankName });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении карты оплаты фермера {UserId}", farmerUserId);
            return Result<GetFarmerPaymentCardDto?>.Fail("Не удалось получить данные карты", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DebitForOrderAsync(int customerUserId, int walletId, decimal amount, int orderId)
    {
        try
        {
            // IDOR-guard: покупатель мог бы передать чужой walletId в
            // CreateOrderDto — без этой проверки заказ бы списал деньги с
            // чужой карты. Проверка происходит внутри AdjustBalanceAsync (на
            // каждой попытке retry, вместе с чтением баланса — без отдельного
            // предварительного round-trip в БД) и сверяет wallet.UserId с уже
            // проверенным владельцем заказа (customerUserId), не с телом запроса.
            var adjusted = await AdjustBalanceAsync(walletId, -amount, WalletTransactionType.Purchase, orderId, expectedOwnerUserId: customerUserId);
            if (!adjusted.IsSuccess)
                return Result<string>.Fail(adjusted.Error!, adjusted.ErrorType!.Value);

            return Result<string>.Ok("Списано");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при списании средств за заказ {OrderId}", orderId);
            return Result<string>.Fail("Не удалось списать средства", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreditFarmerForOrderAsync(int farmerUserId, decimal orderSubtotal, int orderId)
    {
        try
        {
            // Самая старая карта фермера — см. GetFarmerPaymentCardAsync.
            var wallet = (await walletRepository.GetAllByUserIdAsync(farmerUserId)).FirstOrDefault();
            if (wallet is null)
            {
                // У фермера нет карты — не блокируем завершение заказа из-за
                // этого (заказ уже выполнен и оплачен покупателем), просто
                // начисление некуда положить; фермер получит его, как
                // только заведёт карту, вручную это не восстанавливается.
                logger.LogWarning("У фермера {UserId} нет кошелька — начисление за заказ {OrderId} пропущено", farmerUserId, orderId);
                return Result<string>.Ok("У фермера нет кошелька, начисление пропущено");
            }

            var commissionPercentage = await GetApplicableCommissionPercentageAsync();
            var commissionAmount = Math.Round(orderSubtotal * commissionPercentage / 100m, 2, MidpointRounding.AwayFromZero);
            var payout = orderSubtotal - commissionAmount;

            var adjusted = await AdjustBalanceAsync(wallet.Id, payout, WalletTransactionType.FarmerCredit, orderId);
            if (!adjusted.IsSuccess)
                return Result<string>.Fail(adjusted.Error!, adjusted.ErrorType!.Value);

            return Result<string>.Ok("Начислено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при начислении фермеру за заказ {OrderId}", orderId);
            return Result<string>.Fail("Не удалось начислить средства фермеру", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> RefundForOrderAsync(int orderId)
    {
        try
        {
            // Источник истины для "куда возвращать" — исходная Purchase-
            // транзакция этого заказа, а не "кошелёк покупателя" (их теперь
            // может быть несколько). Если её нет — либо заказ оплачен
            // наличными (списания не было вовсе), либо карта уже не
            // существует; в обоих случаях возвращать через Wallet нечего.
            var purchaseTransaction = await walletRepository.FindPurchaseTransactionForOrderAsync(orderId);
            if (purchaseTransaction is null)
            {
                logger.LogInformation("Списания через кошелёк по заказу {OrderId} не найдено — возврат через Wallet пропущен (оплата наличными или карта удалена)", orderId);
                return Result<string>.Ok("Списания через кошелёк не было, возврат не требуется");
            }

            // Amount у Purchase хранится отрицательным (см. AdjustBalanceAsync) —
            // возврат равен модулю исходного списания.
            var refundAmount = -purchaseTransaction.Amount;

            var adjusted = await AdjustBalanceAsync(purchaseTransaction.WalletId, refundAmount, WalletTransactionType.Refund, orderId);
            if (!adjusted.IsSuccess)
                return Result<string>.Fail(adjusted.Error!, adjusted.ErrorType!.Value);

            return Result<string>.Ok("Возврат выполнен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при возврате средств за заказ {OrderId}", orderId);
            return Result<string>.Fail("Не удалось выполнить возврат", ErrorType.InternalServerError);
        }
    }

    // === Единственный путь изменения баланса — атомарность + защита от гонки ===

    // delta > 0 — пополнение/начисление/возврат, delta < 0 — списание.
    // Перечитывает кошелёк заново на каждой попытке (актуальный баланс и
    // Version), чтобы конкурентное изменение всегда учитывалось, а не
    // терялось (lost update) — именно это отличает продуманную реализацию
    // списания от "прочитал баланс один раз — записал минус сумму".
    // expectedOwnerUserId — опциональная IDOR-проверка (см. TopUpAsync,
    // DebitForOrderAsync): выполняется здесь же, на каждой попытке, чтобы не
    // делать отдельный предварительный GetByIdAsync только ради проверки
    // владения — он и так читается на каждой итерации цикла.
    private async Task<Result<Wallet>> AdjustBalanceAsync(int walletId, decimal delta, WalletTransactionType type, int? relatedOrderId, int? expectedOwnerUserId = null)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var wallet = await walletRepository.GetByIdAsync(walletId);
            if (wallet is null)
                return Result<Wallet>.Fail("Кошелёк не найден", ErrorType.NotFound);

            if (expectedOwnerUserId is not null && wallet.UserId != expectedOwnerUserId.Value)
                return Result<Wallet>.Fail("Нет доступа к этой карте", ErrorType.Forbidden);

            var newBalance = wallet.Balance + delta;
            if (newBalance < 0)
                return Result<Wallet>.Fail("Недостаточно средств на балансе", ErrorType.Validation);
            if (newBalance > MaxBalance)
                return Result<Wallet>.Fail($"Итоговый баланс не может превышать {MaxBalance} сомони", ErrorType.Validation);

            wallet.Balance = newBalance;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = type,
                Amount = delta,
                BalanceAfter = newBalance,
                RelatedOrderId = relatedOrderId,
                CreatedAt = DateTime.UtcNow
            };

            var applied = await walletRepository.TryApplyTransactionAsync(wallet, transaction);
            if (applied)
                return Result<Wallet>.Ok(wallet);

            logger.LogWarning("Конфликт конкурентного обновления баланса кошелька {WalletId}, попытка {Attempt}", walletId, attempt);
        }

        logger.LogError("Не удалось обновить баланс кошелька {WalletId} после {Attempts} попыток из-за конкурентных изменений", walletId, MaxConcurrencyRetries);
        return Result<Wallet>.Fail("Слишком много одновременных операций с кошельком, попробуйте ещё раз", ErrorType.Conflict);
    }

    // Комиссия сейчас не привязана к категориям заказа (Order хранит набор
    // разнородных OrderItem, а не одну категорию) — берём платформенную
    // ставку по умолчанию (CategoryId == null), действующую на данный
    // момент. Если ни одной подходящей записи нет — комиссия считается
    // нулевой (фермер получает 100% с продажи), а не блокирует начисление.
    private async Task<decimal> GetApplicableCommissionPercentageAsync()
    {
        var all = await commissionRepository.GetAllAsync();
        var now = DateTime.UtcNow;
        var applicable = all
            .Where(c => c.CategoryId is null && c.EffectiveFrom <= now && (c.EffectiveTo is null || c.EffectiveTo > now))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefault();

        return applicable?.Percentage ?? 0m;
    }

    private static GetWalletDto ToGetDto(Wallet wallet) => new()
    {
        Id = wallet.Id,
        UserId = wallet.UserId,
        CardHolderFirstName = wallet.CardHolderFirstName,
        CardHolderLastName = wallet.CardHolderLastName,
        CardType = wallet.CardType,
        CardNumberLast4 = wallet.CardNumberLast4,
        ExpiryMonth = wallet.ExpiryMonth,
        ExpiryYear = wallet.ExpiryYear,
        BankName = wallet.BankName,
        Balance = wallet.Balance,
        CreatedAt = wallet.CreatedAt,
        UpdatedAt = wallet.UpdatedAt
    };

    private static GetWalletTransactionDto ToTransactionDto(WalletTransaction transaction) => new()
    {
        Id = transaction.Id,
        Type = transaction.Type,
        Amount = transaction.Amount,
        BalanceAfter = transaction.BalanceAfter,
        RelatedOrderId = transaction.RelatedOrderId,
        CreatedAt = transaction.CreatedAt
    };
}
