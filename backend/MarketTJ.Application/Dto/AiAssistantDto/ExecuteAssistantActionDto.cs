namespace MarketTJ.Application.Dto.AiAssistantDto;

public class ExecuteAssistantActionDto
{
    public string Type { get; set; } = null!;
    public Dictionary<string, string> Params { get; set; } = new();
}
