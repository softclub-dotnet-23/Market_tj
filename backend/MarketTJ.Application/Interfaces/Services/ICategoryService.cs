using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.CategoryDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<Result<IEnumerable<GetCategoryDto>>> GetAllAsync();
    Task<Result<GetCategoryDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateCategoryDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateCategoryDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
