using MarketTJ.Application.Dto.AnalyticsDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAnalyticsService
{
    Task<Result<AdminDashboardDto>> GetAdminDashboardAsync();
    Task<Result<FarmerDashboardDto>> GetFarmerDashboardAsync(int farmerId);
}
