namespace MarketTJ.Application.Common;

// Общий контракт для GET-списочных эндпоинтов (раздел 16 ТЗ: "использовать
// pagination"). PagedResult<T> уже существовал в проекте (см. IProductListingService)
// — переиспользован как есть, а не продублирован под новым именем.
public class PagedRequest
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}
