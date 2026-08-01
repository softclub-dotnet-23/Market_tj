using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Infrastructure.Email;

// HTTP API (Resend) вместо прямого SmtpClient — Railway блокирует исходящий
// SMTP-трафик (порт 587), поэтому SmtpEmailSender к smtp.gmail.com никогда
// не подключался с прода (SmtpException: "The operation has timed out" на
// каждый запрос, диагностировано 2026-08-01). Resend работает по HTTPS,
// egress-блокировка на него не действует. Конфигурация — секция "Resend"
// (ApiKey/FromEmail), задаётся через Resend__ApiKey/Resend__FromEmail в
// Railway Variables (или user-secrets локально). SmtpEmailSender оставлен
// в коде для локальной разработки с реальным SMTP — см. DependencyInjection.
public class ResendEmailSender(HttpClient httpClient, IConfiguration configuration, ILogger<ResendEmailSender> logger) : IEmailSender
{
    private const string ApiUrl = "https://api.resend.com/emails";

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var apiKey = configuration["Resend:ApiKey"];
        var from = configuration["Resend:FromEmail"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Resend не настроен (Resend:ApiKey/Resend:FromEmail в конфигурации/переменных окружения)");

        var payload = new JsonObject
        {
            ["from"] = from,
            ["to"] = new JsonArray { toEmail },
            ["subject"] = subject,
            ["html"] = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Content.ReadAsStringAsync();
        var apiMessage = ExtractErrorMessage(responseBody);

        var friendlyMessage = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                $"Resend отклонил запрос: неверный API-ключ (проверьте Resend:ApiKey). {apiMessage}",
            HttpStatusCode.TooManyRequests =>
                $"Resend: превышен лимит отправки писем (rate limit). {apiMessage}",
            HttpStatusCode.UnprocessableEntity =>
                $"Resend отклонил запрос: некорректные данные письма (например, email получателя). {apiMessage}",
            HttpStatusCode.Forbidden =>
                $"Resend: отправка запрещена — домен не подтверждён, либо (при тестовом домене onboarding@resend.dev) получатель не совпадает с адресом аккаунта. {apiMessage}",
            _ =>
                $"Resend API вернул {(int)response.StatusCode} {response.StatusCode}. {apiMessage}"
        };

        logger.LogError("Не удалось отправить письмо через Resend на {Email}: {Message}", toEmail, friendlyMessage);
        throw new InvalidOperationException(friendlyMessage);
    }

    private static string ExtractErrorMessage(string responseBody)
    {
        try
        {
            return JsonNode.Parse(responseBody)?["message"]?.GetValue<string>() ?? responseBody;
        }
        catch
        {
            return responseBody;
        }
    }
}
