namespace MarketTJ.Application.Dto.AiAssistantDto;

public class AskAssistantDto
{
    public string Message { get; set; } = null!;

    // Предыдущие реплики этого диалога (в порядке от старых к новым), которые
    // фронтенд уже показывает в окне чата — без этого поля каждый запрос был
    // изолирован, и ассистент не понимал уточняющих вопросов вроде "а когда?"
    // после предыдущего "статус заказа ORD-123". Опционально: гостевой/первый
    // вопрос отправляется без истории.
    public List<AssistantHistoryMessageDto>? History { get; set; }
}

public class AssistantHistoryMessageDto
{
    // "user" | "assistant" — как в самом чате.
    public string Role { get; set; } = null!;
    public string Text { get; set; } = null!;
}
