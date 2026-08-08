using MarketTJ.Application.Dto.ChatMessageDto;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.WebApi.Filters;
using MarketTJ.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Authorize]
[Route("api/chat-messages")]
public class ChatMessageController(IChatMessageService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => HandleResult(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    // Блок 3 (2026-08-08) — отправка сообщений в чат защищена от спам-кликов
    // "отправить" (>15 сообщений/мин с одного аккаунта).
    [RateLimit]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChatMessageDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadChatMessageRequest request)
        => HandleResult(await service.UploadAsync(request.ConversationId, request.SenderId, request.Caption, request.File.OpenReadStream(), request.File.FileName, request.File.Length));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateChatMessageDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));
}
