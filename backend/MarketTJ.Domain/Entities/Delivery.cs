using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class Delivery
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? CourierId { get; set; }

    // По прямому запросу пользователя (2026-08-05) — фермер может назначить
    // курьера "вручную", не выбирая из зарегистрированных на платформе
    // (например, знакомого водителя/такси). CourierId в этом случае null,
    // а эти два поля хранят контакт. Ровно одно из (CourierId) / (ManualCourierName
    // + ManualCourierPhone) заполнено — проверяется в DeliveryService.
    public string? ManualCourierName { get; set; }
    public string? ManualCourierPhone { get; set; }

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

    // Замена кода подтверждения на фото (2026-08-05, по прямому запросу
    // пользователя) — курьер (или фермер для доставки "вручную") загружает
    // фото у двери клиента, это финальное действие вместо ввода 4-значного
    // кода. Публично доступно покупателю/фермеру/курьеру через GetDeliveryDto.
    public string? DeliveryProofPhotoUrl { get; set; }

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
