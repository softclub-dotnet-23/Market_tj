using System.Security.Cryptography;
using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Код подтверждения email при регистрации (раздел 23 ТЗ дополнен по явному
// запросу пользователя — до этого регистрация была одношаговой, без
// подтверждения). Код хранится хэшированным (BCrypt, как и User.PasswordHash),
// не открытым текстом.
public class EmailVerificationService(
    IEmailVerificationCodeRepository repository,
    IEmailSender emailSender,
    ILogger<EmailVerificationService> logger) : IEmailVerificationService
{
    private const int CodeLength = 6;
    private const int CodeExpiryMinutes = 10;
    private const int ResendCooldownSeconds = 60;
    private const int MaxAttempts = 5;

    // Сколько времени подтверждённый email считается "свежим" для того,
    // чтобы завершить оставшиеся шаги регистрации (ввод пароля/адреса) —
    // без этого окна человеку пришлось бы укладываться в те же 10 минут,
    // что и на сам код.
    private const int VerifiedWindowMinutes = 30;

    public async Task<Result<string>> SendCodeAsync(string email)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var latest = await repository.GetLatestByEmailAsync(normalizedEmail);
            if (latest is not null && (DateTime.UtcNow - latest.CreatedAt).TotalSeconds < ResendCooldownSeconds)
            {
                var waitSeconds = ResendCooldownSeconds - (int)(DateTime.UtcNow - latest.CreatedAt).TotalSeconds;
                return Result<string>.Fail($"Подождите {waitSeconds} сек. перед повторной отправкой кода", ErrorType.Conflict);
            }

            var code = GenerateCode();
            var entity = new EmailVerificationCode
            {
                Email = normalizedEmail,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                Attempts = 0,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpiryMinutes),
                CreatedAt = DateTime.UtcNow
            };
            await repository.AddAsync(entity);

            await emailSender.SendAsync(normalizedEmail, "Код подтверждения Market.tj", BuildEmailBody(code));

            return Result<string>.Ok("Код отправлен на email");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при отправке кода подтверждения на {Email}", email);
            return Result<string>.Fail("Не удалось отправить код подтверждения", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> VerifyCodeAsync(string email, string code)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var latest = await repository.GetLatestByEmailAsync(normalizedEmail);
            if (latest is null)
                return Result<string>.Fail("Код для этого email не запрашивался", ErrorType.Validation);

            if (latest.IsUsed)
                return Result<string>.Fail("Этот код уже использован", ErrorType.Validation);

            if (latest.ExpiresAt < DateTime.UtcNow)
                return Result<string>.Fail("Код истёк, запросите новый", ErrorType.Validation);

            if (latest.Attempts >= MaxAttempts)
                return Result<string>.Fail("Слишком много неверных попыток, запросите новый код", ErrorType.Validation);

            if (!BCrypt.Net.BCrypt.Verify(code, latest.CodeHash))
            {
                latest.Attempts += 1;
                await repository.UpdateAsync(latest);
                return Result<string>.Fail("Неверный код", ErrorType.Validation);
            }

            latest.IsUsed = true;
            await repository.UpdateAsync(latest);

            return Result<string>.Ok("Email подтверждён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при проверке кода подтверждения для {Email}", email);
            return Result<string>.Fail("Не удалось проверить код", ErrorType.InternalServerError);
        }
    }

    public async Task<bool> IsEmailVerifiedAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var latest = await repository.GetLatestByEmailAsync(normalizedEmail);
        return latest is { IsUsed: true } && (DateTime.UtcNow - latest.CreatedAt).TotalMinutes <= VerifiedWindowMinutes;
    }

    // Криптографически случайные 6 цифр (не Random) — предсказуемый ГПСЧ был
    // бы угадываемым, а это код доступа к чужому email/аккаунту.
    private static string GenerateCode()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 1_000_000;
        return value.ToString($"D{CodeLength}");
    }

    private static string BuildEmailBody(string code) =>
        $"""
         <div style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; padding: 24px;">
           <h2 style="color: #1f5731;">Market.tj</h2>
           <p>Ваш код подтверждения для регистрации:</p>
           <p style="font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #1f5731;">{code}</p>
           <p style="color: #666;">Код действителен {CodeExpiryMinutes} минут. Если вы не запрашивали регистрацию на Market.tj — просто проигнорируйте это письмо.</p>
         </div>
         """;
}
