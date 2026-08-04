using MarketTJ.Application.Interfaces.Services;
using MarketTJ.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Authorize(Roles = "Courier,Admin")]
[Route("api/courier-documents")]
public class CourierDocumentController(ICourierDocumentService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => HandleResult(await service.GetAllAsync());

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadCourierDocumentRequest request)
        => HandleResult(await service.UploadAsync(request.CourierProfileId, request.DocumentType, request.File.OpenReadStream(), request.File.FileName, request.File.Length));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));
}
