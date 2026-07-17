using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.SupportTicketDto;

public class GetSupportTicketDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Subject { get; set; } = null!;
    public SupportTicketStatus Status { get; set; }
    public SupportPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? AssignedToAdminId { get; set; }
}
