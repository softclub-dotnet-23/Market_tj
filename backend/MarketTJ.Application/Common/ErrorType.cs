namespace MarketTJ.Application.Common;

public enum ErrorType
{
    NotFound,
    NoChange,
    Validation,
    Conflict,
    Unauthorized,
    // Аутентифицирован, но ресурс принадлежит другому пользователю (IDOR-guard,
    // audit 2026-07-28 находка 2.2) — отдельно от Unauthorized (401 = "нет/просрочен
    // токен"), чтобы фронтенд мог различить "перелогинься" и "это не твоё".
    Forbidden,
    Unknown,
    BadRequest,
    InternalServerError,
    // Внешний провайдер (например, Groq) вернул 429 — отдельно от обычной
    // Validation/Conflict 4xx, чтобы фронтенд мог показать "попробуйте позже"
    // вместо трактовки как ошибки ввода пользователя (2026-08-02, AI-ассистент).
    TooManyRequests
}
