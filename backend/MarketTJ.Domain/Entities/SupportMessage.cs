namespace MarketTJ.Domain.Entities;

public class SupportMessage
{
    public int Id { get; set; }
    public int SupportTicketId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // SupportTicket 1 — many SupportMessage / User 1 — many SupportMessage (как отправитель).
    public SupportTicket SupportTicket { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
