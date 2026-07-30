using MarketTJ.Application.Dto.FarmerDocumentDto;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Authorize(Roles = "Farmer,Admin")]
[Route("api/farmer-documents")]
public class FarmerDocumentController(IFarmerDocumentService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => HandleResult(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFarmerDocumentDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadFarmerDocumentRequest request)
        => HandleResult(await service.UploadAsync(request.FarmerProfileId, request.DocumentType, request.File.OpenReadStream(), request.File.FileName, request.File.Length));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFarmerDocumentDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));
}
