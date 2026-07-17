using MarketTJ.Application.Common;
using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ProductListingDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IProductListingService
{
    // Раздел 19 ТЗ: каталог должен использовать pagination — единственная
    // сущность из 30, где GetAllAsync возвращает PagedResult вместо IEnumerable
    // (см. Constraints присланного промпта). pageNumber/pageSize добавлены как
    // параметры — без них пагинация технически невозможна, промпт-образец их
    // не указал явно для сигнатуры.
    Task<Result<PagedResult<GetProductListingDto>>> GetAllAsync(int pageNumber, int pageSize);
    Task<Result<GetProductListingDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateProductListingDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateProductListingDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
