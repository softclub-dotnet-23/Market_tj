namespace MarketTJ.Application.Interfaces.Services;

// Реальная отправка email (SMTP) — реализация в Infrastructure, Application
// знает только про этот контракт (раздел 12 ТЗ: слоистая архитектура).
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
