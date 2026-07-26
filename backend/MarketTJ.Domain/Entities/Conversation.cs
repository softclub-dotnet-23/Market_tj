namespace MarketTJ.Domain.Entities;

public class Conversation
{
    public int Id { get; set; }
    // OrderId необязателен: чат можно начать и до заказа (вопрос фермеру про
    // товар, ещё ни разу у него не покупая) — тогда пара CustomerId/FarmerId
    // сама по себе якорь чата, а не заказ.
    public int? OrderId { get; set; }
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Order 1 — 0..1 Conversation — необязательная связь (см. комментарий у OrderId).
    public Order? Order { get; set; }

    // Customer/Farmer — раздел 8.15 указывает FK → User напрямую (в отличие от
    // CartItem/Order/Review, где раздел 9 явно требует связь через профили).
    public User Customer { get; set; } = null!;
    public User Farmer { get; set; } = null!;

    // Conversation 1 — many ChatMessage.
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
