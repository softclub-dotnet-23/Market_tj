using MarketTJ.Application.Dto.ReviewDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Отзывы публично видны на странице товара; писать/менять может любой
// вошедший пользователь (без ограничения по роли — сервис пока не проверяет
// владельца, см. отчёт по этой задаче).
[Authorize]
[Route("api/reviews")]
public class ReviewController(IReviewService service) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? farmerId = null)
        => HandleResult(await service.GetAllAsync(farmerId));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    // Ответ фермера на отзыв о себе — доступно фермеру (владение проверяется
    // внутри ReviewService.ReplyAsync) и Admin.
    [Authorize(Roles = "Farmer,Admin")]
    [HttpPatch("{id:int}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyToReviewDto dto)
        => HandleResult(await service.ReplyAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));
}
