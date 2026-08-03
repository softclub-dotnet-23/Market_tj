using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class WalletValidator
{
    // За одну операцию пополнения — разумный верхний предел, а не только
    // "больше нуля": защищает от опечаток (лишний ноль) и абсурдных сумм.
    public const decimal MaxTopUpAmount = 50_000m;

    // Раздел запроса: "до 5 карт на пользователя" — лимит проверяется в
    // WalletService.CreateAsync (нужен доступ к репозиторию), константа
    // живёт здесь как единственный источник истины для сообщения об ошибке.
    public const int MaxCardsPerUser = 5;

    public static Result<string>? ValidateCreate(CreateWalletDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CardHolderFirstName))
            return Result<string>.Fail("Имя держателя карты обязательно", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.CardHolderLastName))
            return Result<string>.Fail("Фамилия держателя карты обязательна", ErrorType.Validation);
        if (dto.CardHolderFirstName.Trim().Length > 100 || dto.CardHolderLastName.Trim().Length > 100)
            return Result<string>.Fail("Имя и фамилия не должны превышать 100 символов", ErrorType.Validation);

        var cardNumber = (dto.CardNumber ?? string.Empty).Replace(" ", "").Trim();
        if (cardNumber.Length != 16 || !cardNumber.All(char.IsDigit))
            return Result<string>.Fail("Номер карты должен состоять ровно из 16 цифр", ErrorType.Validation);
        if (!PassesLuhnCheck(cardNumber))
            return Result<string>.Fail("Номер карты не прошёл проверку (некорректный номер)", ErrorType.Validation);
        if (DetectCardType(cardNumber) is null)
            return Result<string>.Fail("Неподдерживаемый тип карты (поддерживаются Visa, Mastercard, UnionPay)", ErrorType.Validation);

        var cvv = (dto.Cvv ?? string.Empty).Trim();
        if (cvv.Length != 3 || !cvv.All(char.IsDigit))
            return Result<string>.Fail("CVV должен состоять ровно из 3 цифр", ErrorType.Validation);

        if (dto.ExpiryMonth is < 1 or > 12)
            return Result<string>.Fail("Месяц срока действия должен быть от 01 до 12", ErrorType.Validation);

        // Двузначный год (как на реальной карте, формат MM/YY) — храним и
        // сравниваем как полный год, чтобы не завязываться на трактовку "19"
        // vs "2019" где-то ещё в системе.
        var expiryYear = dto.ExpiryYear is >= 0 and <= 99 ? 2000 + dto.ExpiryYear : dto.ExpiryYear;
        var now = DateTime.UtcNow;
        if (expiryYear < now.Year || (expiryYear == now.Year && dto.ExpiryMonth < now.Month))
            return Result<string>.Fail("Срок действия карты истёк", ErrorType.Validation);
        if (expiryYear > now.Year + 15)
            return Result<string>.Fail("Некорректный срок действия карты", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(dto.BankName))
            return Result<string>.Fail("Название банка обязательно", ErrorType.Validation);
        if (dto.BankName.Trim().Length > 200)
            return Result<string>.Fail("Название банка не должно превышать 200 символов", ErrorType.Validation);

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

    // Алгоритм Луна: удваиваем каждую вторую цифру справа, если результат >
    // 9 — вычитаем 9, суммируем все цифры, номер валиден, если сумма кратна 10.
    // Стандартная контрольная сумма номеров банковских карт — используется
    // здесь только для реалистичности формы (это не настоящий платёжный
    // инструмент, см. Wallet.cs), но проверка настоящая.
    public static bool PassesLuhnCheck(string digitsOnly)
    {
        var sum = 0;
        var shouldDouble = false;
        for (var i = digitsOnly.Length - 1; i >= 0; i--)
        {
            var digit = digitsOnly[i] - '0';
            if (shouldDouble)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            shouldDouble = !shouldDouble;
        }
        return sum % 10 == 0;
    }

    // Упрощённое правило по BIN (первым цифрам номера), как в реальных
    // системах: 62 → UnionPay (проверяем раньше "5", т.к. более специфичный
    // префикс), 4 → Visa, 5 → Mastercard. Null — номер не подходит ни под
    // одну поддерживаемую платёжную систему.
    public static CardType? DetectCardType(string digitsOnly)
    {
        if (digitsOnly.StartsWith("62"))
            return CardType.UnionPay;
        if (digitsOnly.StartsWith('4'))
            return CardType.Visa;
        if (digitsOnly.StartsWith('5'))
            return CardType.Mastercard;
        return null;
    }
}
