using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.PlatformSettingsDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class PlatformSettingsServiceTests
{
    private readonly Mock<IAppSettingRepository> _appSettingRepository = new();
    private readonly Mock<ILogger<PlatformSettingsService>> _logger = new();
    private readonly PlatformSettingsService _service;

    public PlatformSettingsServiceTests()
    {
        _service = new PlatformSettingsService(_appSettingRepository.Object, _logger.Object);
        _appSettingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static UpdatePlatformSettingsDto ValidUpdateDto() => new()
    {
        SiteName = "Market.tj",
        LogoUrl = "https://example.com/logo.png",
        ContactEmail = "support@market.tj",
        ContactPhone = "+992900000000",
        CommissionPercent = 12.5m,
        Currency = "TJS",
        MinimumOrderAmount = 50,
        MaintenanceModeEnabled = false,
        MaintenanceMessage = null,
        EmailNotificationsEnabled = true,
        SmsNotificationsEnabled = false
    };

    // ---------- GetAsync ----------

    [Fact]
    public async Task GetAsync_NothingStored_ReturnsDefaults()
    {
        var result = await _service.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Market.tj", result.Data!.SiteName);
        Assert.Equal("TJS", result.Data!.Currency);
        Assert.False(result.Data!.MaintenanceModeEnabled);
        Assert.Null(result.Data!.UpdatedAt);
    }

    [Fact]
    public async Task GetAsync_ExistingSettings_ReturnsMappedValues()
    {
        var now = DateTime.UtcNow;
        _appSettingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new AppSetting { Id = 1, Key = PlatformSettingsKeys.SiteName, Value = "Fresh Market", UpdatedAt = now, UpdatedByAdminId = 1 },
            new AppSetting { Id = 2, Key = PlatformSettingsKeys.CommissionPercent, Value = "8.5", UpdatedAt = now, UpdatedByAdminId = 1 },
            new AppSetting { Id = 3, Key = PlatformSettingsKeys.MaintenanceModeEnabled, Value = "True", UpdatedAt = now, UpdatedByAdminId = 1 }
        ]);

        var result = await _service.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Fresh Market", result.Data!.SiteName);
        Assert.Equal(8.5m, result.Data!.CommissionPercent);
        Assert.True(result.Data!.MaintenanceModeEnabled);
        Assert.Equal(1, result.Data!.UpdatedByAdminId);
    }

    [Fact]
    public async Task GetAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _appSettingRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_NoExistingSettings_CreatesAllKnownKeys()
    {
        var result = await _service.UpdateAsync(ValidUpdateDto(), adminUserId: 1);

        Assert.True(result.IsSuccess);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Exactly(PlatformSettingsKeys.All.Count));
        _appSettingRepository.Verify(r => r.UpdateAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingSettings_UpdatesInPlaceInsteadOfDuplicating()
    {
        var existing = PlatformSettingsKeys.All
            .Select((k, i) => new AppSetting { Id = i + 1, Key = k.Key, Value = "old", UpdatedAt = DateTime.UtcNow })
            .ToList();
        _appSettingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

        var result = await _service.UpdateAsync(ValidUpdateDto(), adminUserId: 2);

        Assert.True(result.IsSuccess);
        _appSettingRepository.Verify(r => r.UpdateAsync(It.IsAny<AppSetting>()), Times.Exactly(PlatformSettingsKeys.All.Count));
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Never);
        Assert.All(existing, s => Assert.Equal(2, s.UpdatedByAdminId));
    }

    [Fact]
    public async Task UpdateAsync_InvalidEmail_ReturnsValidationError()
    {
        var dto = ValidUpdateDto();
        dto.ContactEmail = "not-an-email";

        var result = await _service.UpdateAsync(dto, adminUserId: 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CommissionOutOfRange_ReturnsValidationError()
    {
        var dto = ValidUpdateDto();
        dto.CommissionPercent = 150;

        var result = await _service.UpdateAsync(dto, adminUserId: 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task UpdateAsync_MaintenanceEnabledWithoutMessage_ReturnsValidationError()
    {
        var dto = ValidUpdateDto();
        dto.MaintenanceModeEnabled = true;
        dto.MaintenanceMessage = null;

        var result = await _service.UpdateAsync(dto, adminUserId: 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MaintenanceEnabledWithMessage_Succeeds()
    {
        var dto = ValidUpdateDto();
        dto.MaintenanceModeEnabled = true;
        dto.MaintenanceMessage = "Ведутся технические работы";

        var result = await _service.UpdateAsync(dto, adminUserId: 1);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _appSettingRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(ValidUpdateDto(), adminUserId: 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
