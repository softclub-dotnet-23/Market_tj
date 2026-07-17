using System.Text.Json.Serialization;

namespace MarketTJ.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    [JsonConstructor]
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
        => (Items, TotalCount, Page, PageSize) = (items, totalCount, page, pageSize);

    public static PagedResult<T> Ok(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
        => new(items, totalCount, page, pageSize);
}
