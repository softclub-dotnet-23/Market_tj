using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ProductImageDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IProductImageService
{
    Task<Result<IEnumerable<GetProductImageDto>>> GetAllAsync();
    Task<Result<GetProductImageDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateProductImageDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateProductImageDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
