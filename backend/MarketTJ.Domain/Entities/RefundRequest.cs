using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class RefundRequest
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string Reason { get; set; } = null!;
    public decimal Amount { get; set; }
    public RefundStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int? ProcessedByAdminId { get; set; }

    // Order 1 — 0..many RefundRequest.
    public Order Order { get; set; } = null!;

    // User — Customer, оформивший возврат (раздел 8.21 → User напрямую) /
    // User — Admin, обработавший запрос (необязательная связь).
    public User Customer { get; set; } = null!;
    public User? ProcessedByAdmin { get; set; }
}
