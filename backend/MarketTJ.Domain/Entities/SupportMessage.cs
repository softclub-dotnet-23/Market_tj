namespace MarketTJ.Domain.Entities;

public class SupportMessage
{
    public int Id { get; set; }
    public int SupportTicketId { get; set; }
    // SenderId — null только для самого первого сообщения гостевого тикета
    // (см. SupportTicket.UserId) — сам гость не User. Все остальные сообщения
    // (в т.ч. ответы админа на гостевой тикет) отправляют реальные User.
    public int? SenderId { get; set; }
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // SupportTicket 1 — many SupportMessage / User 1 — many SupportMessage (как отправитель, необязательно).
    public SupportTicket SupportTicket { get; set; } = null!;
    public User? Sender { get; set; }
}
