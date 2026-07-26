using MarketTJ.Application.Dto.ProductImageDto;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Фото объявления публично видно в каталоге; загружает/меняет их владелец
// объявления (Farmer) или Admin.
[Authorize(Roles = "Farmer,Admin")]
[Route("api/product-images")]
public class ProductImageController(IProductImageService service) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => HandleResult(await service.GetAllAsync());

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductImageDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadProductImageRequest request)
        => HandleResult(await service.UploadAsync(request.ProductListingId, request.IsMain, request.File.OpenReadStream(), request.File.FileName, request.File.Length));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductImageDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));
}
