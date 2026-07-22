using MarketTJ.Application.Dto.DeliverySlotDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Слоты доставки выбирает покупатель при оформлении заказа (просмотр — любой
// вошедший), управляет расписанием только Admin. Без класс-уровневого
// [Authorize(Roles=...)], т.к. GetAll/GetById и Create/Update/Delete здесь
// требуют разного (комбинирование class+action [Authorize] складывает
// требования, а не заменяет их).
[Route("api/delivery-slots")]
public class DeliverySlotController(IDeliverySlotService service) : ApiControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => HandleResult(await service.GetAllAsync());

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeliverySlotDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeliverySlotDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));
}
