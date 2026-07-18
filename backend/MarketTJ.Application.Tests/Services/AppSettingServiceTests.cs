using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AppSettingDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class AppSettingServiceTests
{
    private readonly Mock<IAppSettingRepository> _appSettingRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<AppSettingService>> _logger = new();
    private readonly AppSettingService _service;

    public AppSettingServiceTests()
    {
        _service = new AppSettingService(_appSettingRepository.Object, _userRepository.Object, _logger.Object);
        _appSettingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static AppSetting CreateSetting(int id = 1, string key = "DefaultCommissionPercent") => new()
    {
        Id = id,
        Key = key,
        Value = "5",
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateAppSettingDto ValidCreateDto(string key = "DefaultCommissionPercent") => new()
    {
        Key = key,
        Value = "5"
    };

    private static UpdateAppSettingDto ValidUpdateDto(int id = 1, string key = "DefaultCommissionPercent") => new()
    {
        Id = id,
        Key = key,
        Value = "5"
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_SettingsExist_ReturnsMappedDtos()
    {
        _appSettingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateSetting(1), CreateSetting(2, "DefaultDeliveryBasePrice")]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _appSettingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _appSettingRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var setting = CreateSetting(5);
        _appSettingRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(setting);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(setting.Id, result.Data!.Id);
        Assert.Equal(setting.Key, result.Data!.Key);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _appSettingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((AppSetting?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _appSettingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsSettingAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyKey_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Key = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyValue_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Value = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateKey_ReturnsConflict()
    {
        _appSettingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateSetting(1, "DefaultCommissionPercent")]);

        var result = await _service.CreateAsync(ValidCreateDto("DefaultCommissionPercent"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UpdatedByAdminNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);
        var dto = ValidCreateDto();
        dto.UpdatedByAdminId = 5;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UpdatedByAdminNotAdminRole_ReturnsValidationError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { Id = 5, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
        var dto = ValidCreateDto();
        dto.UpdatedByAdminId = 5;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _appSettingRepository.Verify(r => r.AddAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _appSettingRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesSettingAndReturnsOk()
    {
        var setting = CreateSetting(1);
        _appSettingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(setting);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _appSettingRepository.Verify(r => r.UpdateAsync(It.IsAny<AppSetting>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SettingNotFound_ReturnsNotFound()
    {
        _appSettingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((AppSetting?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _appSettingRepository.Verify(r => r.UpdateAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyKey_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Key = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _appSettingRepository.Verify(r => r.UpdateAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateKeyOnAnotherSetting_ReturnsConflict()
    {
        var setting = CreateSetting(1, "A");
        var other = CreateSetting(2, "B");
        _appSettingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(setting);
        _appSettingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([setting, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, "B"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _appSettingRepository.Verify(r => r.UpdateAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UpdatedByAdminNotFound_ReturnsNotFound()
    {
        var setting = CreateSetting(1);
        _appSettingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(setting);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);
        var dto = ValidUpdateDto(1);
        dto.UpdatedByAdminId = 5;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _appSettingRepository.Verify(r => r.UpdateAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _appSettingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingSetting_DeletesAndReturnsOk()
    {
        var setting = CreateSetting(1);
        _appSettingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(setting);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _appSettingRepository.Verify(r => r.DeleteAsync(setting), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SettingNotFound_ReturnsNotFound()
    {
        _appSettingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((AppSetting?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _appSettingRepository.Verify(r => r.DeleteAsync(It.IsAny<AppSetting>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _appSettingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
