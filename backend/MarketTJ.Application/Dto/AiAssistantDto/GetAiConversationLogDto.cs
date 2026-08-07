namespace MarketTJ.Application.Dto.AiAssistantDto;

public class GetAiConversationLogDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserFullName { get; set; }
    public string Role { get; set; } = null!;
    public string Question { get; set; } = null!;
    public string Response { get; set; } = null!;
    public string Intent { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool WasError { get; set; }
}
