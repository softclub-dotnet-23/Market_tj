using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.DeliveryDto;

public class GetDeliveryDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? CourierId { get; set; }
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

    public string? CancellationReason { get; set; }
    public string? ProblemDescription { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Курьер (плоско, чтобы не грузить отдельным запросом на каждой карточке).
    public string? CourierFullName { get; set; }
    public string? CourierPhoneNumber { get; set; }
    public string? CourierAvatarUrl { get; set; }
    public string? CourierTransportType { get; set; }
    public string? CourierVehicleNumber { get; set; }
    public decimal? CourierRating { get; set; }

    // Только для покупателя своего заказа — видит код подтверждения; для
    // всех остальных ролей всегда null (сервер хранит только хэш, см.
    // DeliveryService.ToGetDto).
    public string? ConfirmationCode { get; set; }

    // Сводка по заказу — чтобы курьерский интерфейс ("Мои доставки") не делал
    // отдельный запрос на каждую карточку (audit 2026-08-02).
    public string? OrderNumber { get; set; }
    public string? FarmerName { get; set; }
    public string? FarmerPhoneNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhoneNumber { get; set; }
    public int ItemCount { get; set; }
}
