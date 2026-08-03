using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// PIN — 4 цифры, хэшируется BCrypt (тот же алгоритм, что и пароль пользователя,
// см. AuthService), никогда не хранится и не логируется в открытом виде.
// Защита от брутфорса — 5 неверных попыток подряд блокируют проверку PIN на
// 15 минут (WalletPinLockedUntil на User), с логированием каждой неверной
// попытки. Это ЗАЩИТА ВХОДА В РАЗДЕЛ на клиенте — существующие эндпоинты
// WalletController (GET/topup/transactions) её не используют и не требуют,
// авторизация там как была на JWT, так и осталась (см. IWalletPinService).
public class WalletPinService(
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    ILogger<WalletPinService> logger) : IWalletPinService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    public async Task<Result<WalletPinStatusDto>> GetStatusAsync()
    {
        if (currentUser.UserId is null)
            return Result<WalletPinStatusDto>.Fail("Требуется авторизация", ErrorType.Unauthorized);

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value);
        if (user is null)
            return Result<WalletPinStatusDto>.Fail("Пользователь не найден", ErrorType.NotFound);

        return Result<WalletPinStatusDto>.Ok(new WalletPinStatusDto { IsSet = user.WalletPinHash is not null });
    }

    public async Task<Result<string>> SetPinAsync(SetWalletPinDto dto)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<string>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            if (!IsValidPinFormat(dto.Pin))
                return Result<string>.Fail("PIN должен состоять ровно из 4 цифр", ErrorType.Validation);

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (user.WalletPinHash is not null)
                return Result<string>.Fail("PIN уже установлен — используйте смену PIN", ErrorType.Conflict);

            // Validation, а не Unauthorized/Forbidden — те коды на фронтенде
            // (см. lib/api.ts, request()) перехватываются глобально и всегда
            // подменяются на обобщённое "Требуется авторизация" ДО чтения тела
            // ответа (исторически 401/403 в этом проекте всегда означали
            // "нет валидного токена", никогда "неверные учётные данные для
            // под-ресурса") — с Unauthorized здесь пользователь никогда бы не
            // увидел настоящую причину ("неверный пароль").
            if (string.IsNullOrWhiteSpace(dto.Password) || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Result<string>.Fail("Неверный пароль", ErrorType.Validation);

            user.WalletPinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
            user.WalletPinFailedAttempts = 0;
            user.WalletPinLockedUntil = null;
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);

            return Result<string>.Ok("PIN установлен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при установке PIN кошелька");
            return Result<string>.Fail("Не удалось установить PIN", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> VerifyPinAsync(VerifyWalletPinDto dto)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<string>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (user.WalletPinHash is null)
                return Result<string>.Fail("PIN не установлен", ErrorType.Validation);

            return await CheckPinAndTrackAttemptsAsync(user, dto.Pin);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при проверке PIN кошелька");
            return Result<string>.Fail("Не удалось проверить PIN", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> ChangePinAsync(ChangeWalletPinDto dto)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<string>.Fail("Требуется авторизация", ErrorType.Unauthorized);

            if (!IsValidPinFormat(dto.NewPin))
                return Result<string>.Fail("Новый PIN должен состоять ровно из 4 цифр", ErrorType.Validation);

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (user.WalletPinHash is null)
                return Result<string>.Fail("PIN не установлен — используйте первичную установку", ErrorType.Validation);

            var checkResult = await CheckPinAndTrackAttemptsAsync(user, dto.CurrentPin);
            if (!checkResult.IsSuccess)
                return checkResult;

            user.WalletPinHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPin);
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);

            return Result<string>.Ok("PIN изменён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при смене PIN кошелька");
            return Result<string>.Fail("Не удалось изменить PIN", ErrorType.InternalServerError);
        }
    }

    // Общая логика проверки + учёта попыток для VerifyPinAsync и
    // ChangePinAsync (у смены PIN та же защита от брутфорса для текущего
    // PIN, что и у обычного входа в раздел).
    private async Task<Result<string>> CheckPinAndTrackAttemptsAsync(User user, string pin)
    {
        var now = DateTime.UtcNow;

        // Окно блокировки истекло — сбрасываем счётчик перед тем, как
        // обработать эту попытку как новую, а не считать её сверх старой
        // пятёрки.
        if (user.WalletPinLockedUntil is { } lockedUntil && lockedUntil <= now)
        {
            user.WalletPinFailedAttempts = 0;
            user.WalletPinLockedUntil = null;
        }

        if (user.WalletPinLockedUntil is { } stillLocked && stillLocked > now)
        {
            var minutesLeft = Math.Max(1, (int)Math.Ceiling((stillLocked - now).TotalMinutes));
            return Result<string>.Fail($"Слишком много неверных попыток, попробуйте через {minutesLeft} мин.", ErrorType.TooManyRequests);
        }

        if (!IsValidPinFormat(pin) || !BCrypt.Net.BCrypt.Verify(pin, user.WalletPinHash!))
        {
            user.WalletPinFailedAttempts++;
            logger.LogWarning("Неверная попытка ввода PIN кошелька пользователем {UserId} (попытка {Attempt}/{Max})",
                user.Id, user.WalletPinFailedAttempts, MaxFailedAttempts);

            if (user.WalletPinFailedAttempts >= MaxFailedAttempts)
            {
                user.WalletPinLockedUntil = now.Add(LockDuration);
                await userRepository.UpdateAsync(user);
                logger.LogWarning("PIN кошелька пользователя {UserId} заблокирован на {Minutes} мин. после {Max} неверных попыток",
                    user.Id, LockDuration.TotalMinutes, MaxFailedAttempts);
                return Result<string>.Fail($"Слишком много неверных попыток, попробуйте через {(int)LockDuration.TotalMinutes} мин.", ErrorType.TooManyRequests);
            }

            await userRepository.UpdateAsync(user);
            var remaining = MaxFailedAttempts - user.WalletPinFailedAttempts;
            // Validation — см. комментарий в SetPinAsync про 401/403, глобально
            // перехватываемые фронтендом; тут особенно важно, т.к. именно в
            // этом сообщении передаётся оставшееся число попыток.
            return Result<string>.Fail($"Неверный PIN, осталось попыток: {remaining}", ErrorType.Validation);
        }

        user.WalletPinFailedAttempts = 0;
        user.WalletPinLockedUntil = null;
        await userRepository.UpdateAsync(user);

        return Result<string>.Ok("PIN подтверждён");
    }

    private static bool IsValidPinFormat(string? pin) =>
        !string.IsNullOrEmpty(pin) && pin.Length == 4 && pin.All(char.IsDigit);
}
