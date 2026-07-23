using MarketTJ.Application.Dto.ProductListingDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Объявления — ядро публичного каталога (раздел 4 ТЗ), просмотр анонимный;
// создаёт/меняет владелец (Farmer) или Admin.
[Authorize(Roles = "Farmer,Admin")]
[Route("api/product-listings")]
public class ProductListingController(IProductListingService service) : ApiControllerBase
{
    // IProductListingService.GetAllAsync принимает пагинацию (раздел 19 ТЗ) —
    // единственный сервис в проекте с таким отличием от стандартного CRUD.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        => HandleResult(await service.GetAllAsync(pageNumber, pageSize));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductListingDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductListingDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));
}
