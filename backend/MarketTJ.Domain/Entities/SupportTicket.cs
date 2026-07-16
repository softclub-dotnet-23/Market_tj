using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class SupportTicket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Subject { get; set; } = null!;
    public SupportTicketStatus Status { get; set; }
    public SupportPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? AssignedToAdminId { get; set; }

    // User 1 — many SupportTicket (автор обращения) / User — Admin, назначенный
    // на тикет (необязательная связь).
    public User User { get; set; } = null!;
    public User? AssignedToAdmin { get; set; }

    // SupportTicket 1 — many SupportMessage.
    public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
}
