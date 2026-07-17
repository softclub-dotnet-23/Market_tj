using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// AI-ассистент поиска по каталогу (Anthropic Claude API) — осознанное
// отклонение от раздела 3 ТЗ («В MVP не входят: искусственный интеллект»),
// подтверждено пользователем явно, зафиксировано в TZ_MarketTJ_ClaudeCode.md,
// раздел 38.
public class AiAssistantService(
    HttpClient httpClient,
    IProductListingRepository productListingRepository,
    IConfiguration configuration,
    ILogger<AiAssistantService> logger) : IAiAssistantService
{
    // Актуальная модель — claude-sonnet-5 (в исходном промпте было
    // несуществующее имя claude-sonnet-4-6, исправлено).
    private const string Model = "claude-sonnet-5";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private const string SystemPrompt =
        "Ты ассистент маркетплейса Market.tj. Определи что ищет пользователь, " +
        "вызови search_products с ключевым словом, и верни СТРОГО JSON без markdown: " +
        "{\"intent\":\"product|category|cart|orders|none\",\"productId\":null,\"categoryId\":null,\"message\":\"\"}. " +
        "product — один явный товар. category — несколько товаров одной категории. " +
        "cart/orders — если просит корзину/заказы. none — если не понял, message должен объяснить.";

    public async Task<Result<AssistantResponseDto>> AskAsync(string message)
    {
        try
        {
            var apiKey = configuration["Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogError("Anthropic:ApiKey не задан (appsettings.json / User Secrets)");
                return Result<AssistantResponseDto>.Fail("AI-ассистент временно недоступен", ErrorType.InternalServerError);
            }

            var messages = new JsonArray
            {
                new JsonObject { ["role"] = "user", ["content"] = message }
            };

            var response = await SendToClaudeAsync(apiKey, messages);
            var contentBlocks = response["content"]!.AsArray();

            var toolUseBlock = contentBlocks.FirstOrDefault(b => b!["type"]!.GetValue<string>() == "tool_use");

            if (toolUseBlock is not null)
            {
                var toolUseId = toolUseBlock["id"]!.GetValue<string>();
                var query = toolUseBlock["input"]!["query"]!.GetValue<string>();

                var found = await productListingRepository.SearchAsync(query);
                var toolResultText = found.Count == 0
                    ? "Ничего не найдено"
                    : JsonSerializer.Serialize(found.Select(p => new { p.Id, p.Title, p.RetailPricePerKg }));

                messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = contentBlocks.DeepClone() });
                messages.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "tool_result",
                            ["tool_use_id"] = toolUseId,
                            ["content"] = toolResultText
                        }
                    }
                });

                response = await SendToClaudeAsync(apiKey, messages);
                contentBlocks = response["content"]!.AsArray();
            }

            var textBlock = contentBlocks.FirstOrDefault(b => b!["type"]!.GetValue<string>() == "text");
            if (textBlock is null)
            {
                logger.LogError("Claude не вернул текстовый блок ответа: {Response}", response.ToJsonString());
                return Result<AssistantResponseDto>.Fail("Не удалось получить ответ ассистента", ErrorType.InternalServerError);
            }

            var json = textBlock["text"]!.GetValue<string>().Trim();
            var parsed = JsonSerializer.Deserialize<AssistantResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed is null)
            {
                logger.LogError("Не удалось распарсить JSON от ассистента: {Json}", json);
                return Result<AssistantResponseDto>.Fail("Не удалось разобрать ответ ассистента", ErrorType.InternalServerError);
            }

            return Result<AssistantResponseDto>.Ok(parsed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обращении к AI-ассистенту");
            return Result<AssistantResponseDto>.Fail("Ошибка AI-ассистента", ErrorType.InternalServerError);
        }
    }

    private async Task<JsonObject> SendToClaudeAsync(string apiKey, JsonArray messages)
    {
        var requestBody = new JsonObject
        {
            ["model"] = Model,
            ["max_tokens"] = 1024,
            ["system"] = SystemPrompt,
            ["tools"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "search_products",
                    ["description"] = "Ищет товары в каталоге Market.tj по ключевому слову",
                    ["input_schema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["query"] = new JsonObject { ["type"] = "string" }
                        },
                        ["required"] = new JsonArray { "query" }
                    }
                }
            },
            ["messages"] = messages.DeepClone()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", ApiVersion);

        using var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Anthropic API вернул {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Anthropic API error {response.StatusCode}");
        }

        return JsonNode.Parse(responseBody)!.AsObject();
    }
}
