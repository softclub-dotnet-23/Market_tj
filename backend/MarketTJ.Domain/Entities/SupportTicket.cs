using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class SupportTicket
{
    public int Id { get; set; }
    // UserId — null для обращения от гостя (без регистрации, по прямому
    // запросу пользователя — "чтобы без регистрации тоже могли писать").
    // GuestName/GuestEmail заполняются только в этом случае — ответ админа
    // такому автору уходит на GuestEmail по почте (SupportMessageService),
    // т.к. у гостя нет сессии, куда можно было бы показать ответ в интерфейсе.
    public int? UserId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string Subject { get; set; } = null!;
    public SupportTicketStatus Status { get; set; }
    public SupportPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? AssignedToAdminId { get; set; }

    // User 1 — many SupportTicket (автор обращения, необязательно — см.
    // UserId) / User — Admin, назначенный на тикет (тоже необязательная связь).
    public User? User { get; set; }
    public User? AssignedToAdmin { get; set; }

    // SupportTicket 1 — many SupportMessage.
    public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
}
