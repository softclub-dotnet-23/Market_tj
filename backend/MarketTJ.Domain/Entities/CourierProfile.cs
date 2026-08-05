namespace MarketTJ.Domain.Entities;

public class CourierProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TransportType { get; set; } = null!;
    public string VehicleNumber { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;

    // Точный адрес курьера (по образцу FarmerProfile.Address) — по прямому
    // запросу пользователя (2026-08-05), нужен для точного геокодирования:
    // Region/District одни дают только центр района, а курьер может жить
    // на его окраине — координаты по одному Region/District занижали бы
    // точность 40-километрового радиуса подбора курьера.
    public string? Address { get; set; }

    // Геокодируются через IGoogleGeocodingService при создании/обновлении
    // профиля (адрес/район/регион изменились) — см. CourierProfileService.
    // Null, пока не геокодировано (или если геокодирование не удалось) —
    // такие курьеры не участвуют в подборе по расстоянию (см.
    // DeliveryService.GetAvailableCouriersAsync).
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }

    // Рейтинг курьера — по прямому запросу пользователя (2026-08-02).
    // Сбора оценок от покупателя пока нет (не запрашивалось отдельно), поле
    // хранит текущее значение, по умолчанию 5.0 у новых курьеров.
    public decimal Rating { get; set; } = 5.0m;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // User 1 — 1 CourierProfile.
    public User User { get; set; } = null!;

    // CourierProfile 1 — many Delivery (CourierId у Delivery nullable).
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    // CourierProfile 1 — many CourierDocument (права/техпаспорт для верификации).
    public ICollection<CourierDocument> Documents { get; set; } = new List<CourierDocument>();
}
