namespace MarketTJ.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public int EntityId { get; set; }
    public string Details { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // User 1 — many AuditLog (как Admin). EntityType+EntityId — полиморфная
    // ссылка (раздел 8.19), намеренно не строгий FK — таблица только для чтения.
    public User Admin { get; set; } = null!;
}
