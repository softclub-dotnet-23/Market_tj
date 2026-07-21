using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuditLogDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<AuditLogService>> _logger = new();
    private readonly AuditLogService _service;

    public AuditLogServiceTests()
    {
        _service = new AuditLogService(_auditLogRepository.Object, _userRepository.Object, _logger.Object);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Admin, FullName = "Admin", Email = "a@e.com", PhoneNumber = "1", PasswordHash = "h" });
    }

    private static AuditLog CreateLog(int id = 1, int adminId = 1) => new()
    {
        Id = id,
        AdminId = adminId,
        Action = "Update",
        EntityType = "FarmerProfile",
        EntityId = 1,
        Details = "Verified farmer",
        CreatedAt = DateTime.UtcNow
    };

    private static CreateAuditLogDto ValidCreateDto(int adminId = 1) => new()
    {
        AdminId = adminId,
        Action = "Update",
        EntityType = "FarmerProfile",
        EntityId = 1,
        Details = "Verified farmer"
    };

    private static UpdateAuditLogDto ValidUpdateDto(int id = 1, int adminId = 1) => new()
    {
        Id = id,
        AdminId = adminId,
        Action = "Update",
        EntityType = "FarmerProfile",
        EntityId = 1,
        Details = "Verified farmer"
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_LogsExist_ReturnsMappedDtos()
    {
        _auditLogRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateLog(1), CreateLog(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _auditLogRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _auditLogRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var log = CreateLog(5);
        _auditLogRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(log);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(log.Id, result.Data!.Id);
        Assert.Equal(log.Action, result.Data!.Action);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _auditLogRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((AuditLog?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _auditLogRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsLogAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _auditLogRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroAdminId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.AdminId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _auditLogRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyAction_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Action = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _auditLogRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyEntityType_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.EntityType = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _auditLogRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroEntityId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.EntityId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _auditLogRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyDetails_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Details = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _auditLogRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AdminNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _auditLogRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AdminNotAdminRole_ReturnsValidationError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { Id = 1, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _auditLogRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesLogAndReturnsOk()
    {
        var log = CreateLog(1);
        _auditLogRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(log);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _auditLogRepository.Verify(r => r.UpdateAsync(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_LogNotFound_ReturnsNotFound()
    {
        _auditLogRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((AuditLog?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _auditLogRepository.Verify(r => r.UpdateAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyAction_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Action = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _auditLogRepository.Verify(r => r.UpdateAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AdminNotFound_ReturnsNotFound()
    {
        var log = CreateLog(1);
        _auditLogRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(log);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _auditLogRepository.Verify(r => r.UpdateAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _auditLogRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingLog_DeletesAndReturnsOk()
    {
        var log = CreateLog(1);
        _auditLogRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(log);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _auditLogRepository.Verify(r => r.DeleteAsync(log), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_LogNotFound_ReturnsNotFound()
    {
        _auditLogRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((AuditLog?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _auditLogRepository.Verify(r => r.DeleteAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _auditLogRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
