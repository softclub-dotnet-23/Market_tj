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

    // Загрузка настоящего файла (в отличие от CreateAsync, который принимает
    // уже готовый ImageUrl) — возвращает созданную запись целиком, т.к.
    // фронту сразу нужен и Id (для последующего удаления), и итоговый URL.
    Task<Result<GetProductImageDto>> UploadAsync(int productListingId, bool isMain, Stream fileContent, string fileName, long fileSizeBytes);
}
