using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Без [Authorize] на уровне класса — гости тоже должны иметь возможность
// спросить ассистента (явный запрос пользователя), не только авторизованные
// покупатели. Ask() сам решает по роли из токена (если он есть), какие
// инструменты доступны — гостю/покупателю только поиск по каталогу.
[Route("api/ai-assistant")]
public class AiAssistantController(IAiAssistantService aiAssistantService) : ApiControllerBase
{
    // Контроллер не содержит бизнес-логику (раздел 19 ТЗ) — только вызов
    // сервиса и приведение Result<T> к единому формату ответа (раздел 20).
    // Блок 3 (2026-08-08) — [RateLimit] здесь не действует на гостей (их
    // некому банить), но защищает авторизованных пользователей от спама
    // "отправить" в чате ассистента.
    [RateLimit]
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskAssistantDto dto)
        => HandleResult(await aiAssistantService.AskAsync(dto.Message, dto.History));

    // В отличие от Ask() — требует авторизации: здесь выполняется реальная
    // мутация (изменение цены/статуса объявления, решение по жалобе),
    // предложенная ассистентом и подтверждённая пользователем на фронтенде.
    [Authorize]
    [HttpPost("execute-action")]
    public async Task<IActionResult> ExecuteAction([FromBody] ExecuteAssistantActionDto dto)
        => HandleResult(await aiAssistantService.ExecuteActionAsync(dto));
}
