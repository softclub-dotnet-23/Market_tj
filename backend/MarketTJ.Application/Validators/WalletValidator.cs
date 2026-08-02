using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class WalletValidator
{
    // За одну операцию пополнения — разумный верхний предел, а не только
    // "больше нуля": защищает от опечаток (лишний ноль) и абсурдных сумм.
    public const decimal MaxTopUpAmount = 50_000m;

    public static Result<string>? ValidateCreate(CreateWalletDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CardHolderFirstName))
            return Result<string>.Fail("Имя держателя карты обязательно", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.CardHolderLastName))
            return Result<string>.Fail("Фамилия держателя карты обязательна", ErrorType.Validation);
        if (dto.CardHolderFirstName.Trim().Length > 100 || dto.CardHolderLastName.Trim().Length > 100)
            return Result<string>.Fail("Имя и фамилия не должны превышать 100 символов", ErrorType.Validation);
        if (!Enum.IsDefined(dto.CardType))
            return Result<string>.Fail("Указан несуществующий тип карты", ErrorType.Validation);

        return null;
    }

    public static Result<string>? ValidateTopUp(TopUpWalletDto dto)
    {
        if (dto.Amount <= 0)
            return Result<string>.Fail("Сумма пополнения должна быть больше нуля", ErrorType.Validation);

        // Никаких 1000.999 — ровно 2 знака после запятой (сомони, как и
        // любая другая денежная сумма в проекте, не дробится мельче копейки).
        if (dto.Amount != Math.Round(dto.Amount, 2))
            return Result<string>.Fail("Сумма должна быть округлена до 2 знаков после запятой", ErrorType.Validation);

        if (dto.Amount > MaxTopUpAmount)
            return Result<string>.Fail($"Максимальная сумма пополнения за одну операцию — {MaxTopUpAmount} сомони", ErrorType.Validation);

        return null;
    }
}
