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

    public DateTime? EstimatedPickupAt { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? FarmerNote { get; set; }
    public string? ClientNote { get; set; }
    public string? AdminNote { get; set; }
    public string? CourierNote { get; set; }

    // По прямому запросу пользователя (2026-08-02) — 4-значный код
    // подтверждения доставки. ConfirmationCode хранится в открытом виде
    // ТОЛЬКО чтобы покупатель мог посмотреть его повторно в любой момент —
    // сервис отдаёт это поле исключительно покупателю (владельцу заказа), не
    // курьеру/фермеру/админу. Сама сверка при подтверждении идёт по
    // ConfirmationCodeHash (курьер никогда не получает код напрямую).
    public string? ConfirmationCode { get; set; }
    public string? ConfirmationCodeHash { get; set; }
    public int ConfirmationAttempts { get; set; }

    public string? CancellationReason { get; set; }
    public string? ProblemDescription { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Order 1 — 0..1 Delivery (со стороны Delivery связь обязательная).
    public Order Order { get; set; } = null!;

    // CourierProfile 1 — many Delivery; CourierId nullable — доставка может
    // быть ещё не назначена (DeliveryStatus.Pending).
    public CourierProfile? Courier { get; set; }
}
