using MarketTJ.Application.Common;
using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IProductListingRepository
{
    Task<List<ProductListing>> GetAllAsync();
    Task<ProductListing?> GetByIdAsync(int id);

    // Поиск по названию/описанию объявления и названию продукта — нужен для
    // AI-ассистента (tool search_products). Не кэшируется — запрос произвольный,
    // TTL по хэшу запроса усложнил бы метод сверх текущей задачи.
    Task<List<ProductListing>> SearchAsync(string query);

    // Раздел 13.5 ТЗ: публичный каталог с фильтрами/сортировкой/пагинацией —
    // всё выполняется в БД (WHERE/ORDER BY/OFFSET-FETCH), а не Skip/Take по
    // уже загруженному в память списку (см. GetAllAsync выше — тот остаётся
    // как есть, им пользуется админка, где нужны ВСЕ статусы, а не только
    // Active). Rating/OrderCount — агрегаты по подзапросам (Reviews по
    // FarmerId, OrderItems по ProductListingId), их нет как колонок.
    Task<(List<(ProductListing Listing, double Rating, int OrderCount)> Items, int TotalCount)> SearchCatalogAsync(ProductListingSearchFilter filter);

    // Различные регионы среди активных объявлений — для выпадающего списка
    // фильтра "Регион" в каталоге, чтобы не тянуть все объявления на фронт
    // только ради списка уникальных значений.
    Task<List<string>> GetDistinctActiveRegionsAsync();

    // Обогащение read-эндпоинтов объявлений (GetAll/GetById) агрегатами,
    // которые раньше считал фронт, стянув себе /order-items и /reviews
    // целиком (audit 2026-08-02). Только для переданных id/farmerId, не для
    // всего каталога — тот же принцип, что и у SearchCatalogAsync выше.
    Task<Dictionary<int, int>> GetOrderCountsByListingIdsAsync(List<int> listingIds);
    Task<Dictionary<int, double>> GetRatingsByFarmerIdsAsync(List<int> farmerIds);

    // Для карточек категорий каталога (сколько активных объявлений подтверждённых
    // фермеров в каждой категории) — раньше фронт считал это, стянув себе весь
    // список объявлений и фильтруя в браузере.
    Task<Dictionary<int, int>> GetActiveListingCountsByCategoryAsync();

    // Для публичной витрины фермеров (/farmer-profiles/public) — количество
    // активных объявлений и id категорий, в которых продаёт фермер (для тегов
    // на карточке фермера).
    Task<Dictionary<int, (int Count, List<int> CategoryIds)>> GetActiveListingStatsByFarmerIdsAsync(List<int> farmerIds);

    Task AddAsync(ProductListing productListing);
    Task UpdateAsync(ProductListing productListing);
    Task DeleteAsync(ProductListing productListing);
}
