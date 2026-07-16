namespace MarketTJ.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    // Order 1 — 0..1 Review (со стороны Review — обязательная связь).
    public Order Order { get; set; } = null!;

    // User 1 — many Review (как Customer / как Farmer).
    public User Customer { get; set; } = null!;
    public User Farmer { get; set; } = null!;
}
