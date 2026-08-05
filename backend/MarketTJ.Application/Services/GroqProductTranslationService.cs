using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Автоперевод названия/описания товара на недостающие языки (ru/tj/en) —
// по прямому запросу пользователя (2026-08-05), тот же провайдер (Groq),
// что и AI-ассистент (см. AiAssistantService), но отдельный простой
// non-tool-calling запрос — переводу не нужны ни история диалога, ни
// function calling. Fail-open по конструкции: TranslateMissingAsync
// НИКОГДА не бросает исключение — при недоступности/квоте/невалидном
// ответе Groq просто возвращает null для непереведённых полей, а
// ProductListingService (вызывающий код) не блокирует сохранение
// объявления из-за этого — недостающие языки останутся пустыми, фермер
// сможет дозаполнить их вручную через Edit.
public class GroqProductTranslationService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GroqProductTranslationService> logger) : IProductTranslationService
{
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "llama-3.3-70b-versatile";

    public async Task<ProductTranslationOutput> TranslateMissingAsync(ProductTranslationInput input)
    {
        var missingTitle = string.IsNullOrWhiteSpace(input.TitleRu) || string.IsNullOrWhiteSpace(input.TitleTj) || string.IsNullOrWhiteSpace(input.TitleEn);
        var missingDescription = HasAnyDescription(input) &&
            (string.IsNullOrWhiteSpace(input.DescriptionRu) || string.IsNullOrWhiteSpace(input.DescriptionTj) || string.IsNullOrWhiteSpace(input.DescriptionEn));

        if (!missingTitle && !missingDescription)
            return new ProductTranslationOutput(input.TitleRu, input.TitleTj, input.TitleEn, input.DescriptionRu, input.DescriptionTj, input.DescriptionEn);

        var apiKey = configuration["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogError("Groq:ApiKey не задан (appsettings.json / User Secrets) — перевод объявления пропущен");
            return new ProductTranslationOutput(input.TitleRu, input.TitleTj, input.TitleEn, input.DescriptionRu, input.DescriptionTj, input.DescriptionEn);
        }

        try
        {
            var prompt = BuildPrompt(input);
            var requestBody = new JsonObject
            {
                ["model"] = Model,
                ["temperature"] = 0.2,
                ["response_format"] = new JsonObject { ["type"] = "json_object" },
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "system",
                        ["content"] = "Ты переводчик для маркетплейса сельхозпродукции Таджикистана. Переводи названия и описания товаров между русским, таджикским и английским языками. Отвечай ТОЛЬКО валидным JSON без пояснений.",
                    },
                    new JsonObject { ["role"] = "user", ["content"] = prompt },
                },
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await httpClient.SendAsync(request);
            var rawBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Groq API (перевод объявления) вернул {StatusCode}: {Body}", response.StatusCode, rawBody);
                return new ProductTranslationOutput(input.TitleRu, input.TitleTj, input.TitleEn, input.DescriptionRu, input.DescriptionTj, input.DescriptionEn);
            }

            var content = JsonNode.Parse(rawBody)?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(content))
            {
                logger.LogWarning("Groq не вернул текстовый ответ для перевода объявления: {Body}", rawBody);
                return new ProductTranslationOutput(input.TitleRu, input.TitleTj, input.TitleEn, input.DescriptionRu, input.DescriptionTj, input.DescriptionEn);
            }

            var translated = JsonNode.Parse(content);
            return new ProductTranslationOutput(
                TitleRu: FirstNonEmpty(input.TitleRu, translated?["titleRu"]?.GetValue<string>()),
                TitleTj: FirstNonEmpty(input.TitleTj, translated?["titleTj"]?.GetValue<string>()),
                TitleEn: FirstNonEmpty(input.TitleEn, translated?["titleEn"]?.GetValue<string>()),
                DescriptionRu: FirstNonEmpty(input.DescriptionRu, translated?["descriptionRu"]?.GetValue<string>()),
                DescriptionTj: FirstNonEmpty(input.DescriptionTj, translated?["descriptionTj"]?.GetValue<string>()),
                DescriptionEn: FirstNonEmpty(input.DescriptionEn, translated?["descriptionEn"]?.GetValue<string>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при переводе объявления через Groq API");
            return new ProductTranslationOutput(input.TitleRu, input.TitleTj, input.TitleEn, input.DescriptionRu, input.DescriptionTj, input.DescriptionEn);
        }
    }

    private static bool HasAnyDescription(ProductTranslationInput input) =>
        !string.IsNullOrWhiteSpace(input.DescriptionRu) || !string.IsNullOrWhiteSpace(input.DescriptionTj) || !string.IsNullOrWhiteSpace(input.DescriptionEn);

    private static string? FirstNonEmpty(string? existing, string? translated) =>
        !string.IsNullOrWhiteSpace(existing) ? existing : (string.IsNullOrWhiteSpace(translated) ? null : translated);

    private static string BuildPrompt(ProductTranslationInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Даны поля названия/описания товара на русском (Ru), таджикском (Tj) и английском (En) языках — некоторые могут быть пустыми.");
        sb.AppendLine("Переведи КАЖДОЕ пустое поле с любого из непустых полей того же типа (title или description).");
        sb.AppendLine("Не изменяй уже заполненные поля — верни их как есть.");
        sb.AppendLine("Верни JSON строго с ключами: titleRu, titleTj, titleEn, descriptionRu, descriptionTj, descriptionEn.");
        sb.AppendLine();
        sb.AppendLine($"titleRu: {input.TitleRu ?? "(пусто)"}");
        sb.AppendLine($"titleTj: {input.TitleTj ?? "(пусто)"}");
        sb.AppendLine($"titleEn: {input.TitleEn ?? "(пусто)"}");
        sb.AppendLine($"descriptionRu: {input.DescriptionRu ?? "(пусто)"}");
        sb.AppendLine($"descriptionTj: {input.DescriptionTj ?? "(пусто)"}");
        sb.AppendLine($"descriptionEn: {input.DescriptionEn ?? "(пусто)"}");
        return sb.ToString();
    }
}
