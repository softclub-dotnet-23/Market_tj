namespace MarketTJ.Application.Dto.ProductListingDto;

// Раздел 13.5 ТЗ: результат публичного поиска по каталогу — тот же набор
// полей, что и GetProductListingDto, плюс Rating/OrderCount, посчитанные
// сервером (см. ProductListingRepository.SearchCatalogAsync) — раньше их
// вычислял фронт, стягивая себе /reviews и /order-items целиком.
public class GetCatalogListingDto : GetProductListingDto
{
    public double Rating { get; set; }
    public int OrderCount { get; set; }
}
