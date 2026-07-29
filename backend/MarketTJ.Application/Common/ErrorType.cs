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
    InternalServerError
}
