using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // User 1 — 1 FarmerProfile / CustomerProfile / CourierProfile / 0..1 FarmerStaffMember.
    public FarmerProfile? FarmerProfile { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }
    public CourierProfile? CourierProfile { get; set; }
    public FarmerStaffMember? FarmerStaffMember { get; set; }

    // User 1 — many Notification.
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    // User 1 — many Favorite (как Customer) — ProductListing many-many User.
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    // User 1 — many ChatMessage / SupportMessage (как отправитель).
    public ICollection<ChatMessage> SentChatMessages { get; set; } = new List<ChatMessage>();
    public ICollection<SupportMessage> SentSupportMessages { get; set; } = new List<SupportMessage>();

    // User 1 — many SupportTicket (как автор обращения).
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

    // User 1 — many AuditLog (как Admin).
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
