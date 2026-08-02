namespace MarketTJ.Application.Dto.AiAssistantDto;

// Intent для покупателя/гостя: "product" (один явный товар) | "category"
// (несколько товаров одной категории) | "cart" | "orders" | "none" (не понял
// запрос — тогда заполнен Message). Для фермера/админа (раздел 38 ТЗ,
// расширение 2026-08-01): "info" (ответ на вопрос по своим данным, только
// Message) | "action_pending" (предлагает действие — заполнен Action,
// выполняется только через POST /ai-assistant/execute-action после
// подтверждения пользователем). "text" (2026-08-02) — служебный intent,
// который выставляет сам бэкенд (не модель), когда ответ Groq не удалось
// разобрать как JSON, но он похож на настоящий связный ответ — см.
// AiAssistantService.AskAsync, raw-text fallback. На фронтенде ведёт себя
// как "none"/"info": просто текстовый пузырь без специальных кнопок.
public class AssistantResponseDto
{
    public string Intent { get; set; } = null!;

    // Id конкретного объявления (ProductListing, раздел 8.7 ТЗ) — соответствует
    // маршруту /product/:id на фронтенде (раздел 14.1 ТЗ), а не абстрактному
    // типу продукта (Product, раздел 8.6).
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public string Message { get; set; } = null!;
    public AssistantActionDto? Action { get; set; }
}
