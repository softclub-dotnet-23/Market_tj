namespace MarketTJ.Application.Dto.ConversationDto;

public class CreateConversationDto
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public bool IsClosed { get; set; }
}
