using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Authorize]
[Route("api/deliveries")]
public class DeliveryController(IDeliveryService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => HandleResult(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeliveryDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));

    // Полноценное назначение и отслеживание курьера (audit 2026-08-02) —
    // права на каждый метод дополнительно проверяются внутри DeliveryService
    // (никогда не доверяем только фронтенд-проверке роли).

    [HttpGet("by-order/{orderId:int}")]
    public async Task<IActionResult> GetByOrderId(int orderId)
        => HandleResult(await service.GetByOrderIdAsync(orderId));

    [Authorize(Roles = "Courier")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyDeliveries()
        => HandleResult(await service.GetMyDeliveriesAsync());

    // По прямому запросу пользователя (2026-08-05): назначение курьера —
    // исключительно фермер, Admin в этом больше не участвует ни основным, ни
    // запасным путём (роль на контроллере — грубая проверка, точная внутри
    // DeliveryService, та же схема, что и везде в этом контроллере/проекте).
    [Authorize(Roles = "Farmer")]
    [HttpGet("available-couriers")]
    public async Task<IActionResult> GetAvailableCouriers(
        [FromQuery] int orderId,
        [FromQuery] bool onlyAvailable = false,
        [FromQuery] string? region = null,
        [FromQuery] string? district = null,
        [FromQuery] string? transportType = null,
        [FromQuery] decimal? minRating = null)
        => HandleResult(await service.GetAvailableCouriersAsync(new AvailableCourierFilter
        {
            OrderId = orderId,
            OnlyAvailable = onlyAvailable,
            Region = region,
            District = district,
            TransportType = transportType,
            MinRating = minRating,
        }));

    [Authorize(Roles = "Farmer")]
    [HttpPost("by-order/{orderId:int}/assign")]
    public async Task<IActionResult> AssignCourier(int orderId, [FromBody] AssignCourierDto dto)
        => HandleResult(await service.AssignCourierAsync(orderId, dto));

    // Курьер "вручную" (не зарегистрирован на платформе) — только имя и
    // телефон, см. AssignManualCourierDto/AssignManualCourierAsync.
    [Authorize(Roles = "Farmer")]
    [HttpPost("by-order/{orderId:int}/assign-manual")]
    public async Task<IActionResult> AssignManualCourier(int orderId, [FromBody] AssignManualCourierDto dto)
        => HandleResult(await service.AssignManualCourierAsync(orderId, dto));

    [Authorize(Roles = "Farmer")]
    [HttpPost("{id:int}/confirm-manual")]
    public async Task<IActionResult> ConfirmManualDelivery(int id, [FromForm] ConfirmDeliveryPhotoRequest request)
        => HandleResult(await service.ConfirmManualDeliveryAsync(id, request.Photo.OpenReadStream(), request.Photo.FileName, request.Photo.Length));

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/admin-details")]
    public async Task<IActionResult> UpdateAdminDetails(int id, [FromBody] UpdateDeliveryAdminDetailsDto dto)
        => HandleResult(await service.UpdateAdminDetailsAsync(id, dto));

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelDeliveryDto dto)
        => HandleResult(await service.CancelAsync(id, dto));

    [Authorize(Roles = "Farmer")]
    [HttpPost("by-order/{orderId:int}/mark-ready")]
    public async Task<IActionResult> MarkReadyForPickup(int orderId)
        => HandleResult(await service.MarkReadyForPickupAsync(orderId));

    // Курьер отменяет СВОЮ уже назначенную доставку (Блок 2, 2026-08-08) —
    // причина обязательна, DTO переиспользован от админского CancelAsync.
    [Authorize(Roles = "Courier")]
    [HttpPost("{id:int}/courier-cancel")]
    public async Task<IActionResult> CourierCancel(int id, [FromBody] CancelDeliveryDto dto)
        => HandleResult(await service.CancelByCourierAsync(id, dto.Reason));

    [Authorize(Roles = "Courier")]
    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
        => HandleResult(await service.AcceptAsync(id));

    [Authorize(Roles = "Courier")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateCourierStatus(int id, [FromBody] CourierStatusUpdateDto dto)
        => HandleResult(await service.UpdateCourierStatusAsync(id, dto));

    [Authorize(Roles = "Courier")]
    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id, [FromForm] ConfirmDeliveryPhotoRequest request)
        => HandleResult(await service.ConfirmDeliveryAsync(id, request.Photo.OpenReadStream(), request.Photo.FileName, request.Photo.Length));

    [HttpPost("{id:int}/report-problem")]
    public async Task<IActionResult> ReportProblem(int id, [FromBody] ReportDeliveryProblemDto dto)
        => HandleResult(await service.ReportProblemAsync(id, dto));
}
