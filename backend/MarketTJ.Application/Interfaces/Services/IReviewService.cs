using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ReviewDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IReviewService
{
    // farmerId — опциональный фильтр (страница фермера/товара запрашивает
    // отзывы только о конкретном фермере, вместо того чтобы тянуть их все и
    // фильтровать в браузере, см. audit 2026-08-02). Admin-панель вызывает
    // без параметра и получает полный список, как раньше.
    Task<Result<IEnumerable<GetReviewDto>>> GetAllAsync(int? farmerId = null);
    Task<Result<GetReviewDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateReviewDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateReviewDto dto);
    Task<Result<string>> DeleteAsync(int id);

    // Ответ фермера на отзыв о себе — отдельный метод, а не часть UpdateAsync
    // (тот принадлежит автору отзыва — покупателю; здесь наоборот, пишет
    // фермер, которому отзыв адресован).
    Task<Result<string>> ReplyAsync(int id, ReplyToReviewDto dto);
}
