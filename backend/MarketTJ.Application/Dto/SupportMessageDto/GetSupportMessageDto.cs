namespace MarketTJ.Application.Dto.SupportMessageDto;

public class GetSupportMessageDto
{
    public int Id { get; set; }
    public int SupportTicketId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
