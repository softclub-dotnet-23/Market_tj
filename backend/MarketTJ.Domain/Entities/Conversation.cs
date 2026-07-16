namespace MarketTJ.Domain.Entities;

public class Conversation
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Order 1 — 0..1 Conversation (со стороны Conversation — обязательная связь).
    public Order Order { get; set; } = null!;

    // Customer/Farmer — раздел 8.15 указывает FK → User напрямую (в отличие от
    // CartItem/Order/Review, где раздел 9 явно требует связь через профили).
    public User Customer { get; set; } = null!;
    public User Farmer { get; set; } = null!;

    // Conversation 1 — many ChatMessage.
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
