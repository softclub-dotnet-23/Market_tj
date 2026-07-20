using MarketTJ.Application.Dto.AnalyticsDto;

namespace MarketTJ.Application.Interfaces.Repositories;

// Отдельный репозиторий (не CRUD) — инкапсулирует агрегационные LINQ-запросы
// к AppDbContext (GroupBy/Sum/Count на уровне БД), которые не укладываются в
// generic-паттерн GetAllAsync/GetByIdAsync остальных репозиториев проекта.
public interface IAnalyticsRepository
{
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    Task<FarmerDashboardDto> GetFarmerDashboardAsync(int farmerProfileId);
}
