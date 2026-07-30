using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IEmailVerificationService
{
    // Генерирует и отправляет код на email. Result содержит просто
    // подтверждающее сообщение — сам код наружу никогда не возвращается.
    Task<Result<string>> SendCodeAsync(string email);

    Task<Result<string>> VerifyCodeAsync(string email, string code);

    // AuthService.RegisterAsync вызывает это перед созданием User — раздел
    // "email не подтверждён" в самой регистрации не даёт создать аккаунт
    // без прохождения кода. Не выбрасывает, просто bool — сама регистрация
    // решает, каким Result-ом это обернуть.
    Task<bool> IsEmailVerifiedAsync(string email);
}
