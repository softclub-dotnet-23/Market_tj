using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductListingDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Объявления — ядро публичного каталога (раздел 4 ТЗ), просмотр анонимный;
// создаёт/меняет владелец (Farmer) или Admin.
[Authorize(Roles = "Farmer,Admin")]
[Route("api/product-listings")]
public class ProductListingController(IProductListingService service) : ApiControllerBase
{
    // IProductListingService.GetAllAsync принимает пагинацию (раздел 19 ТЗ) —
    // единственный сервис в проекте с таким отличием от стандартного CRUD.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        => HandleResult(await service.GetAllAsync(pageNumber, pageSize));

    // Раздел 13.5 ТЗ: публичный поиск по каталогу — фильтры/сортировка/
    // пагинация целиком на бэкенде (см. ProductListingSearchFilter). Отдельно
    // от GetAll выше — тот отдаёт объявления любого статуса (нужно админке),
    // здесь всегда только Active + в наличии.
    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> SearchCatalog(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? categoryIds = null,
        [FromQuery] string? region = null,
        [FromQuery] int? farmerId = null,
        [FromQuery] decimal? priceMin = null,
        [FromQuery] decimal? priceMax = null,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "popularity",
        [FromQuery] string? listingIds = null)
    {
        var filter = new ProductListingSearchFilter
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            CategoryIds = ParseIntList(categoryIds),
            Region = region,
            FarmerProfileId = farmerId,
            PriceMin = priceMin,
            PriceMax = priceMax,
            Search = search,
            SortBy = sortBy,
            ListingIds = ParseIntList(listingIds),
        };
        return HandleResult(await service.SearchCatalogAsync(filter));
    }

    [AllowAnonymous]
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions()
        => HandleResult(await service.GetDistinctActiveRegionsAsync());

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => HandleResult(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductListingDto dto)
        => HandleResult(await service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductListingDto dto)
        => HandleResult(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await service.DeleteAsync(id));

    // categoryIds/listingIds приходят как "1,2,3" из query-string — единого
    // биндера списка int из CSV в ASP.NET Core нет, поэтому парсим вручную.
    private static List<int>? ParseIntList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return null;

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToList();
    }
}
