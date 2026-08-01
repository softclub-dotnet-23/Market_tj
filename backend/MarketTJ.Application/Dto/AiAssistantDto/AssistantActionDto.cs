namespace MarketTJ.Application.Dto.AiAssistantDto;

// Ассистент только ПРЕДЛАГАЕТ действие (intent="action_pending") — сам не
// выполняет мутацию. Фронтенд показывает ConfirmLabel с кнопкой подтверждения,
// и только по явному клику пользователя вызывается POST /ai-assistant/execute-action
// с этими же Type/Params. Права (владение объявлением, роль Admin) проверяются
// заново на сервере в момент выполнения, а не на основании того, что сказал AI.
public class AssistantActionDto
{
    public string Type { get; set; } = null!;
    public Dictionary<string, string> Params { get; set; } = new();
    public string ConfirmLabel { get; set; } = null!;
}
