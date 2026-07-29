using MarketTJ.Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Infrastructure.Persistence.Seed;

// Audit 2026-07-28, находка 2.1: до фикса POST/PUT /api/users писал
// dto.PasswordHash в User.PasswordHash напрямую, без BCrypt.HashPassword —
// т.е. хранил присланный пароль как есть. Перехэшировать "вслепую" нельзя:
// нет достоверного способа отличить "это plaintext" от "это уже валидный,
// просто короткий BCrypt-хэш" по одной длине. Безопасный вариант (как и
// рекомендовано в отчёте) — деактивировать такие учётки и потребовать смены
// пароля через администратора, а не гадать. Идемпотентно: уже деактивированные
// или уже корректно захэшированные учётки не трогает при повторных запусках.
public static class PlaintextPasswordFixup
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(PlaintextPasswordFixup));

        var users = await userRepository.GetAllAsync();
        var affected = 0;

        foreach (var user in users)
        {
            if (!user.IsActive || LooksLikeBCryptHash(user.PasswordHash))
                continue;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);
            affected++;

            logger.LogWarning(
                "Пользователь {UserId} ({Email}) деактивирован: PasswordHash не похож на BCrypt-хэш " +
                "(вероятно, создан/обновлён через POST/PUT /api/users до фикса находки 2.1). Требуется ручной сброс пароля админом.",
                user.Id, user.Email);
        }

        if (affected > 0)
            logger.LogWarning("PlaintextPasswordFixup: деактивировано {Count} учётных записей с нехэшированным паролем", affected);
    }

    private static bool LooksLikeBCryptHash(string hash) =>
        hash.Length == 60 &&
        (hash.StartsWith("$2a$", StringComparison.Ordinal) ||
         hash.StartsWith("$2b$", StringComparison.Ordinal) ||
         hash.StartsWith("$2y$", StringComparison.Ordinal));
}
