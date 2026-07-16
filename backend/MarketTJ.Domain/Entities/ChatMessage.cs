namespace MarketTJ.Domain.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Conversation 1 — many ChatMessage / User 1 — many ChatMessage (как отправитель).
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
