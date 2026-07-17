using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ProductDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IProductService
{
    Task<Result<IEnumerable<GetProductDto>>> GetAllAsync();
    Task<Result<GetProductDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateProductDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateProductDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
