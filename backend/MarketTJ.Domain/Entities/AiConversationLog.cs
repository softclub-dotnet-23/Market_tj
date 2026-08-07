namespace MarketTJ.Domain.Entities;

// Журнал вопрос/ответ AI-ассистента (2026-08-08, по явному запросу
// пользователя) — для последующего анализа админом, где ассистент чаще
// всего не справляется (WasError=true). UserId nullable — гость тоже может
// спрашивать ассистента (см. AiAssistantController.Ask, без [Authorize]).
public class AiConversationLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Role { get; set; } = null!;
    public string Question { get; set; } = null!;
    public string Response { get; set; } = null!;
    public string Intent { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool WasError { get; set; }

    public User? User { get; set; }
}
