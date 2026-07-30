using System.Net;
using System.Net.Mail;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace MarketTJ.Infrastructure.Email;

// Конфигурация — секция "Smtp" (Host/Port/User/Password/From/FromName).
// Учётные данные хранятся через dotnet user-secrets в разработке (не в
// appsettings.json — это реальный пароль приложения Gmail, не локальный
// dev-пароль от Postgres), в проде — переменные окружения/секреты хостинга.
public class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var section = configuration.GetSection("Smtp");
        var host = section["Host"];
        var port = int.Parse(section["Port"] ?? "587");
        var user = section["User"];
        var password = section["Password"];
        var from = section["From"] ?? user;
        var fromName = section["FromName"] ?? "Market.tj";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SMTP не настроен (секция Smtp в конфигурации/user-secrets пуста)");

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, password),
            // Дефолтный Timeout у SmtpClient — 100 000 мс (раздел "почему так
            // долго грузится" — ответ админа в поддержке ждал письмо до 100
            // секунд, прежде чем сам код успевал поймать исключение и всё
            // равно сохранить ответ). 10 секунд достаточно для обычной
            // отправки через Gmail; если SMTP реально недоступен — лучше
            // быстро отдать ошибку в лог и не блокировать сам ответ.
            Timeout = 10_000
        };

        using var message = new MailMessage
        {
            From = new MailAddress(from!, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}
