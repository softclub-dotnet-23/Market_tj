using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Interfaces.Services;
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

    // Роль на контроллере — грубая проверка, точная (владелец заказа для
    // Farmer, без ограничений для Admin) внутри DeliveryService — та же
    // схема, что и везде в этом контроллере/проекте.
    [Authorize(Roles = "Farmer,Admin")]
    [HttpGet("available-couriers")]
    public async Task<IActionResult> GetAvailableCouriers(
        [FromQuery] bool onlyAvailable = false,
        [FromQuery] string? region = null,
        [FromQuery] string? district = null,
        [FromQuery] string? transportType = null,
        [FromQuery] decimal? minRating = null)
        => HandleResult(await service.GetAvailableCouriersAsync(new AvailableCourierFilter
        {
            OnlyAvailable = onlyAvailable,
            Region = region,
            District = district,
            TransportType = transportType,
            MinRating = minRating,
        }));

    // Фермер назначает курьера сам (2026-08-04) — Admin сохраняет право как
    // запасной вариант, если фермер недоступен/не успевает.
    [Authorize(Roles = "Farmer,Admin")]
    [HttpPost("by-order/{orderId:int}/assign")]
    public async Task<IActionResult> AssignCourier(int orderId, [FromBody] AssignCourierDto dto)
        => HandleResult(await service.AssignCourierAsync(orderId, dto));

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/admin-details")]
    public async Task<IActionResult> UpdateAdminDetails(int id, [FromBody] UpdateDeliveryAdminDetailsDto dto)
        => HandleResult(await service.UpdateAdminDetailsAsync(id, dto));

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelDeliveryDto dto)
        => HandleResult(await service.CancelAsync(id, dto));

    [Authorize(Roles = "Farmer,Admin")]
    [HttpPost("by-order/{orderId:int}/mark-ready")]
    public async Task<IActionResult> MarkReadyForPickup(int orderId)
        => HandleResult(await service.MarkReadyForPickupAsync(orderId));

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
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmDeliveryDto dto)
        => HandleResult(await service.ConfirmDeliveryAsync(id, dto));

    [HttpPost("{id:int}/report-problem")]
    public async Task<IActionResult> ReportProblem(int id, [FromBody] ReportDeliveryProblemDto dto)
        => HandleResult(await service.ReportProblemAsync(id, dto));
}
