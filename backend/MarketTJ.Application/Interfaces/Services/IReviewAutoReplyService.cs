namespace MarketTJ.Application.Interfaces.Services;

// Отдельный от IAiAssistantService интерфейс (не переиспользуем его) —
// AiAssistantService уже зависит от IReviewService (get_reviews_about_me,
// propose_reply_review), а ReviewService.CreateAsync должен вызывать
// генерацию автоответа — зависимость ReviewService → IAiAssistantService →
// IReviewService была бы циклической на уровне DI. Этот сервис — простой
// одноразовый вызов Groq без tool-calling, ни от чего доменного не зависит.
public interface IReviewAutoReplyService
{
    Task<string?> GenerateReplyAsync(int rating, string? comment);
}
