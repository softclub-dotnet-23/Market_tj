namespace MarketTJ.Application.Dto.ChatMessageDto;

public class CreateChatMessageDto
{
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
}
