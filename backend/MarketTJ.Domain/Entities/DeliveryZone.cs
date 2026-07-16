namespace MarketTJ.Domain.Entities;

public class DeliveryZone
{
    public int Id { get; set; }
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public decimal? PricePerKm { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // DeliveryZone 1 — many Delivery — раздел 9: связь через совпадение
    // Region/District, необязательный FK, поэтому в Delivery нет обязательной
    // ссылки на DeliveryZone (см. Delivery.cs) — здесь навигационной
    // коллекции намеренно нет, чтобы не создавать несуществующий строгий FK.
}
