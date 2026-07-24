using MarketTJ.Application.Dto.AnalyticsDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAnalyticsService
{
    Task<Result<AdminDashboardDto>> GetAdminDashboardAsync();
    // Раздел 16 ТЗ: farmerId больше не принимается от клиента — параметр это
    // UserId авторизованного фермера (из JWT-claims), профиль резолвится внутри.
    Task<Result<FarmerDashboardDto>> GetFarmerDashboardAsync(int userId);
}
