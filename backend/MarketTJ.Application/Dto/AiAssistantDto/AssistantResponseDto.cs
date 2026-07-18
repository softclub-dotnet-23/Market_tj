namespace MarketTJ.Application.Dto.AiAssistantDto;

// Intent: "product" (один явный товар) | "category" (несколько товаров одной
// категории) | "cart" | "orders" | "none" (не понял запрос — тогда заполнен Message).
public class AssistantResponseDto
{
    public string Intent { get; set; } = null!;

    // Id конкретного объявления (ProductListing, раздел 8.7 ТЗ) — соответствует
    // маршруту /product/:id на фронтенде (раздел 14.1 ТЗ), а не абстрактному
    // типу продукта (Product, раздел 8.6).
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public string Message { get; set; } = null!;
}
