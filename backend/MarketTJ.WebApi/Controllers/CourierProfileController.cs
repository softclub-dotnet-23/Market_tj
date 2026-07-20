using MarketTJ.Application.Dto.CourierProfileDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Route("api/courier-profiles")]
public class CourierProfileController(ICourierProfileService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => HandleResult(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourierProfileDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCourierProfileDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));
}
