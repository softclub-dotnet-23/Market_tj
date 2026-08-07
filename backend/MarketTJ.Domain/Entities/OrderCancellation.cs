namespace MarketTJ.Domain.Entities;

// Причина отмены заказа курьером/фермером (Блок 2, 2026-08-08, по явному
// запросу пользователя) — единая таблица для обеих ролей (Role различает),
// вместо двух параллельных CourierCancellation/FarmerCancellation: логика
// подсчёта нарушений за 24ч и эскалации бана одна и та же для обеих ролей,
// раздельные таблицы только продублировали бы её.
public class OrderCancellation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = null!;
    public int OrderId { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
