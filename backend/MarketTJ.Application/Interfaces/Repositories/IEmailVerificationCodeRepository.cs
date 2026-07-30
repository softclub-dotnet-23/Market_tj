using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IEmailVerificationCodeRepository
{
    // Последняя запись для email — нужна и для отправки (антиспам-пауза
    // между попытками), и для проверки (сверяем именно последний код).
    Task<EmailVerificationCode?> GetLatestByEmailAsync(string email);
    Task AddAsync(EmailVerificationCode code);
    Task UpdateAsync(EmailVerificationCode code);
}
