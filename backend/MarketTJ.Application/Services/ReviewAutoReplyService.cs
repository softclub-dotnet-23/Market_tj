using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Автоответ фермера на отзыв — по прямому запросу пользователя (2026-08-02):
// "если включена галочка — AI сам отвечает на отзывы сразу после того, как
// их оставили". Один прямой запрос к Groq без tool-calling (не переиспользует
// инфраструктуру AiAssistantService — см. комментарий в IReviewAutoReplyService).
public class ReviewAutoReplyService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<ReviewAutoReplyService> logger) : IReviewAutoReplyService
{
    private const string Model = "llama-3.3-70b-versatile";
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    private const string SystemPrompt =
        "Ты пишешь короткий, тёплый и уместный ответ фермера на отзыв покупателя о его " +
        "хозяйстве на маркетплейсе Market.tj. Учитывай оценку (от 1 до 5 звёзд) и текст " +
        "комментария: искренне благодари за хороший отзыв, вежливо и без оправданий " +
        "реагируй на критику. Отвечай на том же языке, на котором написан комментарий " +
        "покупателя (если комментария нет — на русском). 1-3 коротких предложения, без " +
        "markdown и без кавычек вокруг всего ответа, от первого лица (\"мы\"/\"я\"). Верни " +
        "только сам текст ответа, ничего больше.";

    // null, если ключ не настроен или запрос не удался — вызывающий код
    // (ReviewService.CreateAsync) должен молча пропустить автоответ в этом
    // случае, а не ронять создание самого отзыва.
    public async Task<string?> GenerateReplyAsync(int rating, string? comment)
    {
        var apiKey = configuration["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Groq:ApiKey не задан — автоответ на отзыв пропущен");
            return null;
        }

        try
        {
            var userPrompt = $"Оценка: {rating}/5. Комментарий покупателя: " +
                (string.IsNullOrWhiteSpace(comment) ? "(без комментария)" : comment);

            var requestBody = new JsonObject
            {
                ["model"] = Model,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = SystemPrompt },
                    new JsonObject { ["role"] = "user", ["content"] = userPrompt },
                },
                ["temperature"] = 0.6,
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Groq API вернул {StatusCode} при автоответе на отзыв: {Body}", response.StatusCode, responseBody);
                return null;
            }

            var text = JsonNode.Parse(responseBody)?["choices"]?.AsArray().FirstOrDefault()?["message"]?["content"]?.GetValue<string>();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim().Trim('"');
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при генерации автоответа на отзыв");
            return null;
        }
    }
}
