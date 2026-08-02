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

    // По прямому запросу пользователя (2026-08-02): фермер может ответить на
    // отзыв о себе (вручную или текст предлагает AI-ассистент — см.
    // AiAssistantService.propose_reply_review). Null, пока ответа нет.
    public string? FarmerReply { get; set; }
    public DateTime? FarmerRepliedAt { get; set; }

    // Order 1 — 0..1 Review (со стороны Review — обязательная связь).
    public Order Order { get; set; } = null!;

    // FarmerProfile 1 — many Review; CustomerId — по аналогии с CartItem/Order
    // (раздел 9 TZ1.md), снова через профили, не напрямую через User.
    public CustomerProfile Customer { get; set; } = null!;
    public FarmerProfile Farmer { get; set; } = null!;
}
