namespace MarketTJ.Domain.Entities;

public class DeliverySlot
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime Date { get; set; }
    public string TimeFrom { get; set; } = null!;
    public string TimeTo { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Order 1 — 0..1 DeliverySlot (со стороны DeliverySlot — обязательная связь).
    public Order Order { get; set; } = null!;
}
