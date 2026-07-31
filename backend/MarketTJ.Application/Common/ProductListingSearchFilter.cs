namespace MarketTJ.Application.Common;

// Раздел 13.5 ТЗ: параметры публичного поиска по каталогу — все опциональны
// (null/пусто = фильтр не применяется). SortBy — "popularity" (по умолчанию,
// количество заказанных позиций), "price-asc", "price-desc", "rating",
// "fresh" (по дате создания объявления).
public class ProductListingSearchFilter
{
    public List<int>? CategoryIds { get; set; }
    public string? Region { get; set; }
    public int? FarmerProfileId { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "popularity";

    // "Избранное" хранится на фронте (localStorage), а не в БД — при
    // фильтре "только избранное" фронт присылает свои Id, бэкенд просто
    // сужает выборку до них, комбинируя с остальными фильтрами через AND.
    public List<int>? ListingIds { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
