using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ReviewDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IReviewService
{
    Task<Result<IEnumerable<GetReviewDto>>> GetAllAsync();
    Task<Result<GetReviewDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateReviewDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateReviewDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
