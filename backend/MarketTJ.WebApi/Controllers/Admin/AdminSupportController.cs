using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Dto.SupportMessageDto;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin/support-tickets")]
public class AdminSupportController(ISupportTicketService ticketService, ISupportMessageService messageService, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] PagedRequest request, [FromQuery] SupportTicketStatus? status)
        => HandleResult(await ticketService.GetPagedAsync(request, status));

    [HttpGet("{id:int}/messages")]
    public async Task<IActionResult> GetMessages(int id)
        => HandleResult(await messageService.GetByTicketIdAsync(id));

    // Обычный ответ в переписку — не входит в перечень "activate/deactivate/
    // approve/reject/status change" из задачи, поэтому в AuditLog не пишется.
    [HttpPost("{id:int}/messages")]
    public async Task<IActionResult> Reply(int id, [FromBody] AdminReplyMessageDto dto)
        => HandleResult(await messageService.CreateAsync(new CreateSupportMessageDto
        {
            SupportTicketId = id,
            SenderId = currentUser.UserId!.Value,
            Message = dto.Message
        }));
}
