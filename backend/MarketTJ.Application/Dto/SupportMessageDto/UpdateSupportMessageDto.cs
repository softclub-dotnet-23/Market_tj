namespace MarketTJ.Application.Dto.SupportMessageDto;

public class UpdateSupportMessageDto
{
    public int Id { get; set; }
    public int SupportTicketId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = null!;
}
