namespace MarketTJ.Application.Dto.SupportMessageDto;

public class CreateSupportMessageDto
{
    public int SupportTicketId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = null!;
}
