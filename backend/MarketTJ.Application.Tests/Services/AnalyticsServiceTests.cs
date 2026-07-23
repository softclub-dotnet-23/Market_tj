using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AnalyticsDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<IAnalyticsRepository> _analyticsRepository = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<ILogger<AnalyticsService>> _logger = new();
    private readonly AnalyticsService _service;

    public AnalyticsServiceTests()
    {
        _service = new AnalyticsService(_analyticsRepository.Object, _farmerProfileRepository.Object, _logger.Object);
    }

    private static FarmerProfile CreateFarmerProfile(int id = 1, int userId = 10) => new()
    {
        Id = id,
        UserId = userId,
        FarmName = "Farm",
        Region = "Хатлон",
        District = "Бохтар",
        Village = "V",
        Address = "A",
        VerificationStatus = FarmerVerificationStatus.Verified
    };

    // ---------- GetAdminDashboardAsync ----------

    [Fact]
    public async Task GetAdminDashboardAsync_ReturnsDashboardFromRepository()
    {
        var dashboard = new AdminDashboardDto { TotalUsers = 5 };
        _analyticsRepository.Setup(r => r.GetAdminDashboardAsync()).ReturnsAsync(dashboard);

        var result = await _service.GetAdminDashboardAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data!.TotalUsers);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _analyticsRepository.Setup(r => r.GetAdminDashboardAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAdminDashboardAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetFarmerDashboardAsync ----------
    // Раздел 16 ТЗ: userId приходит из JWT-claims, а не из query — сервис сам
    // резолвит FarmerProfile по UserId (см. AdminController/AnalyticsController).

    [Fact]
    public async Task GetFarmerDashboardAsync_UserHasFarmerProfile_ResolvesProfileAndReturnsDashboard()
    {
        var profile = CreateFarmerProfile(id: 3, userId: 10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(profile);
        _analyticsRepository.Setup(r => r.GetFarmerDashboardAsync(3)).ReturnsAsync(new FarmerDashboardDto { TotalOwnProducts = 7 });

        var result = await _service.GetFarmerDashboardAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Data!.TotalOwnProducts);
        _analyticsRepository.Verify(r => r.GetFarmerDashboardAsync(3), Times.Once);
    }

    [Fact]
    public async Task GetFarmerDashboardAsync_UserHasNoFarmerProfile_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.GetFarmerDashboardAsync(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _analyticsRepository.Verify(r => r.GetFarmerDashboardAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetFarmerDashboardAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetFarmerDashboardAsync(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
