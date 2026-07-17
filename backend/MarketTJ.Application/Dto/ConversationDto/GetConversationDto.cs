namespace MarketTJ.Application.Dto.ConversationDto;

public class GetConversationDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
