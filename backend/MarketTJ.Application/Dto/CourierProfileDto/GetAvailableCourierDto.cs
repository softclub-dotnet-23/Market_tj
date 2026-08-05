namespace MarketTJ.Application.Dto.CourierProfileDto;

// Карточка курьера в drawer'е назначения (Admin) — audit 2026-08-02.
public class GetAvailableCourierDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string TransportType { get; set; } = null!;
    public string VehicleNumber { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public decimal Rating { get; set; }
    public bool IsAvailable { get; set; }
    public int ActiveDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }

    // Реальное расстояние (Haversine) от курьера до адреса доставки заказа —
    // заменяет прежнюю сортировку "тот же район первым" (2026-08-05). Список
    // уже отфильтрован по ≤40 км на бэкенде (см. DeliveryService.
    // GetAvailableCouriersAsync), отсортирован по возрастанию.
    public double DistanceKm { get; set; }
}
