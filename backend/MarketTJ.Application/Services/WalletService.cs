using System.Security.Cryptography;
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
// Один пользователь = одна карта (unique index на Wallet.UserId — см.
// WalletConfiguration). Все изменения баланса проходят через единственный
// путь — AdjustBalanceAsync — и всегда сопровождаются строкой в
// WalletTransaction (полный аудит-лог, не просто текущее число).
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
    // оптимистичной блокировки (xmin), прежде чем сдаться — два реальных
    // одновременных списания с одного и того же кошелька случаются редко
    // даже под нагрузкой, этого запаса достаточно.
    private const int MaxConcurrencyRetries = 5;

    public async Task<Result<GetWalletDto?>> GetMyWalletAsync()
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<GetWalletDto?>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            var wallet = await walletRepository.GetByUserIdAsync(currentUser.UserId.Value);
            return Result<GetWalletDto?>.Ok(wallet is null ? null : ToGetDto(wallet));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении кошелька");
            return Result<GetWalletDto?>.Fail("Не удалось получить кошелёк", ErrorType.InternalServerError);
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

            // Быстрая проверка до похода в БД на запись — не заменяет unique
            // index (см. TryAddAsync ниже), а лишь избавляет от лишнего
            // round-trip в подавляющем большинстве (не гоночных) случаев.
            var existing = await walletRepository.GetByUserIdAsync(currentUser.UserId.Value);
            if (existing is not null)
                return Result<GetWalletDto>.Fail("У вас уже есть карта", ErrorType.Conflict);

            var wallet = new Wallet
            {
                UserId = currentUser.UserId.Value,
                CardHolderFirstName = dto.CardHolderFirstName.Trim(),
                CardHolderLastName = dto.CardHolderLastName.Trim(),
                CardType = dto.CardType,
                CardNumberLast4 = GenerateLast4(),
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Финальный, надёжный барьер против гонки (два параллельных
            // запроса на создание карты одним пользователем) — unique index
            // в БД, TryAddAsync возвращает false вместо падения с
            // исключением, если он сработал.
            var created = await walletRepository.TryAddAsync(wallet);
            if (!created)
                return Result<GetWalletDto>.Fail("У вас уже есть карта", ErrorType.Conflict);

            return Result<GetWalletDto>.Ok(ToGetDto(wallet));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании кошелька");
            return Result<GetWalletDto>.Fail("Не удалось создать карту", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetWalletDto>> TopUpAsync(TopUpWalletDto dto)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<GetWalletDto>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            var validation = WalletValidator.ValidateTopUp(dto);
            if (validation is not null)
                return Result<GetWalletDto>.Fail(validation.Error!, validation.ErrorType!.Value);

            var wallet = await walletRepository.GetByUserIdAsync(currentUser.UserId.Value);
            if (wallet is null)
                return Result<GetWalletDto>.Fail("Карта не найдена — сначала создайте её", ErrorType.NotFound);

            var adjusted = await AdjustBalanceAsync(wallet.Id, dto.Amount, WalletTransactionType.TopUp, null);
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

    public async Task<Result<IEnumerable<GetWalletTransactionDto>>> GetMyTransactionsAsync()
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<IEnumerable<GetWalletTransactionDto>>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            var wallet = await walletRepository.GetByUserIdAsync(currentUser.UserId.Value);
            if (wallet is null)
                return Result<IEnumerable<GetWalletTransactionDto>>.Ok([]);

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
            var wallet = await walletRepository.GetByUserIdAsync(farmerUserId);
            return Result<GetFarmerPaymentCardDto?>.Ok(wallet is null
                ? null
                : new GetFarmerPaymentCardDto { CardType = wallet.CardType, CardNumberLast4 = wallet.CardNumberLast4 });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении карты оплаты фермера {UserId}", farmerUserId);
            return Result<GetFarmerPaymentCardDto?>.Fail("Не удалось получить данные карты", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DebitForOrderAsync(int customerUserId, decimal amount, int orderId)
    {
        try
        {
            var wallet = await walletRepository.GetByUserIdAsync(customerUserId);
            if (wallet is null)
                return Result<string>.Fail("У покупателя нет кошелька — оформите карту перед заказом", ErrorType.Validation);

            var adjusted = await AdjustBalanceAsync(wallet.Id, -amount, WalletTransactionType.Purchase, orderId);
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
            var wallet = await walletRepository.GetByUserIdAsync(farmerUserId);
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

    public async Task<Result<string>> RefundForOrderAsync(int customerUserId, decimal amount, int orderId)
    {
        try
        {
            var wallet = await walletRepository.GetByUserIdAsync(customerUserId);
            if (wallet is null)
            {
                logger.LogWarning("У покупателя {UserId} нет кошелька — возврат за заказ {OrderId} пропущен", customerUserId, orderId);
                return Result<string>.Ok("У покупателя нет кошелька, возврат пропущен");
            }

            var adjusted = await AdjustBalanceAsync(wallet.Id, amount, WalletTransactionType.Refund, orderId);
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
    // xmin), чтобы конкурентное изменение всегда учитывалось, а не терялось
    // (lost update) — именно это отличает продуманную реализацию списания
    // от "прочитал баланс один раз — записал минус сумму".
    private async Task<Result<Wallet>> AdjustBalanceAsync(int walletId, decimal delta, WalletTransactionType type, int? relatedOrderId)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var wallet = await walletRepository.GetByIdAsync(walletId);
            if (wallet is null)
                return Result<Wallet>.Fail("Кошелёк не найден", ErrorType.NotFound);

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
    // нулевой (фермер получает 100% с продажи), а не блокирует начисление:
    // платформа без настроенной комиссии — легитимное состояние (см. живую
    // проверку AI-ассистента этой же сессии: "Комиссии не настроены").
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

    // Криптографически случайные 4 цифры — никак не производные от UserId и
    // не связаны с реальным номером (полного номера у карты нет вовсе, см.
    // комментарий в Wallet.cs) — заполняют "•••• 1234" в UI правдоподобным,
    // непредсказуемым значением.
    private static string GenerateLast4()
    {
        Span<byte> buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(buffer);
        return string.Concat(buffer.ToArray().Select(b => (b % 10).ToString()));
    }

    private static GetWalletDto ToGetDto(Wallet wallet) => new()
    {
        Id = wallet.Id,
        UserId = wallet.UserId,
        CardHolderFirstName = wallet.CardHolderFirstName,
        CardHolderLastName = wallet.CardHolderLastName,
        CardType = wallet.CardType,
        CardNumberLast4 = wallet.CardNumberLast4,
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
