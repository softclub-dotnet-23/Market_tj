using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AnalyticsDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class AnalyticsService(
    IAnalyticsRepository analyticsRepository,
    IFarmerProfileRepository farmerProfileRepository,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    public async Task<Result<AdminDashboardDto>> GetAdminDashboardAsync()
    {
        try
        {
            var dashboard = await analyticsRepository.GetAdminDashboardAsync();
            return Result<AdminDashboardDto>.Ok(dashboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении дашборда аналитики администратора");
            return Result<AdminDashboardDto>.Fail("Не удалось получить данные аналитики", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<FarmerDashboardDto>> GetFarmerDashboardAsync(int userId)
    {
        try
        {
            var farmerProfile = await farmerProfileRepository.GetByUserIdAsync(userId);
            if (farmerProfile is null)
                return Result<FarmerDashboardDto>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            var dashboard = await analyticsRepository.GetFarmerDashboardAsync(farmerProfile.Id);
            return Result<FarmerDashboardDto>.Ok(dashboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении дашборда аналитики фермера (userId={UserId})", userId);
            return Result<FarmerDashboardDto>.Fail("Не удалось получить данные аналитики фермера", ErrorType.InternalServerError);
        }
    }
}
