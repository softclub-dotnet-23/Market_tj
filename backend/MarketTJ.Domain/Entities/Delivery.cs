using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class Delivery
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? CourierId { get; set; }
    public string PickupAddress { get; set; } = null!;
    public string DeliveryAddress { get; set; } = null!;
    public decimal DeliveryPrice { get; set; }
    public DeliveryStatus Status { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Order 1 — 0..1 Delivery (со стороны Delivery связь обязательная).
    public Order Order { get; set; } = null!;

    // CourierProfile 1 — many Delivery; CourierId nullable — доставка может
    // быть ещё не назначена (DeliveryStatus.Pending).
    public CourierProfile? Courier { get; set; }
}
