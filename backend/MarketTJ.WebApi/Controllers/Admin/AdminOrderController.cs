using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin")]
public class AdminOrderController(IOrderService orderService, IRefundRequestService refundRequestService, ICurrentUserService currentUser)
    : ApiControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] PagedRequest request, [FromQuery] OrderStatus? status)
        => HandleResult(await orderService.GetPagedAsync(request, status));

    [HttpPatch("orders/{id:int}/status")]
    public async Task<IActionResult> ChangeOrderStatus(int id, [FromBody] ChangeOrderStatusDto dto)
        => HandleResult(await orderService.ChangeStatusAsync(id, dto.Status, currentUser.UserId!.Value));

    [HttpGet("refund-requests")]
    public async Task<IActionResult> GetRefundRequests([FromQuery] PagedRequest request, [FromQuery] RefundStatus? status)
        => HandleResult(await refundRequestService.GetPagedAsync(request, status));

    [HttpPatch("refund-requests/{id:int}/approve")]
    public async Task<IActionResult> ApproveRefundRequest(int id)
        => HandleResult(await refundRequestService.ApproveAsync(id, currentUser.UserId!.Value));

    [HttpPatch("refund-requests/{id:int}/reject")]
    public async Task<IActionResult> RejectRefundRequest(int id)
        => HandleResult(await refundRequestService.RejectAsync(id, currentUser.UserId!.Value));
}
