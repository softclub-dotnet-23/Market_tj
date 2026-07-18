using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DailySalesSnapshotDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class DailySalesSnapshotServiceTests
{
    private readonly Mock<IDailySalesSnapshotRepository> _dailySalesSnapshotRepository = new();
    private readonly Mock<ILogger<DailySalesSnapshotService>> _logger = new();
    private readonly DailySalesSnapshotService _service;

    public DailySalesSnapshotServiceTests()
    {
        _service = new DailySalesSnapshotService(_dailySalesSnapshotRepository.Object, _logger.Object);
        _dailySalesSnapshotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static DailySalesSnapshot CreateSnapshot(int id = 1, DateTime? date = null) => new()
    {
        Id = id,
        Date = date ?? DateTime.UtcNow.Date,
        TotalOrders = 10,
        TotalRevenue = 1000,
        TotalCommission = 50,
        NewFarmers = 1,
        NewCustomers = 2,
        CompletedDeliveries = 8,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateDailySalesSnapshotDto ValidCreateDto(DateTime? date = null) => new()
    {
        Date = date ?? DateTime.UtcNow.Date,
        TotalOrders = 10,
        TotalRevenue = 1000,
        TotalCommission = 50,
        NewFarmers = 1,
        NewCustomers = 2,
        CompletedDeliveries = 8
    };

    private static UpdateDailySalesSnapshotDto ValidUpdateDto(int id = 1, DateTime? date = null) => new()
    {
        Id = id,
        Date = date ?? DateTime.UtcNow.Date,
        TotalOrders = 10,
        TotalRevenue = 1000,
        TotalCommission = 50,
        NewFarmers = 1,
        NewCustomers = 2,
        CompletedDeliveries = 8
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_SnapshotsExist_ReturnsMappedDtos()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateSnapshot(1), CreateSnapshot(2, DateTime.UtcNow.Date.AddDays(-1))]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var snapshot = CreateSnapshot(5);
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(snapshot);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(snapshot.Id, result.Data!.Id);
        Assert.Equal(snapshot.TotalRevenue, result.Data!.TotalRevenue);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DailySalesSnapshot?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsSnapshotAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DefaultDate_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Date = default;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeTotalOrders_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.TotalOrders = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeTotalRevenue_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.TotalRevenue = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeTotalCommission_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.TotalCommission = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeNewFarmers_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.NewFarmers = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeNewCustomers_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.NewCustomers = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeCompletedDeliveries_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CompletedDeliveries = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateDate_ReturnsConflict()
    {
        var date = DateTime.UtcNow.Date;
        _dailySalesSnapshotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateSnapshot(1, date)]);

        var result = await _service.CreateAsync(ValidCreateDto(date));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.AddAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesSnapshotAndReturnsOk()
    {
        var snapshot = CreateSnapshot(1);
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(snapshot);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, snapshot.Date));

        Assert.True(result.IsSuccess);
        _dailySalesSnapshotRepository.Verify(r => r.UpdateAsync(It.IsAny<DailySalesSnapshot>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SnapshotNotFound_ReturnsNotFound()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DailySalesSnapshot?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.UpdateAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NegativeTotalOrders_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.TotalOrders = -5;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.UpdateAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateDateOnAnotherSnapshot_ReturnsConflict()
    {
        var date1 = DateTime.UtcNow.Date;
        var date2 = DateTime.UtcNow.Date.AddDays(-1);
        var snapshot = CreateSnapshot(1, date1);
        var other = CreateSnapshot(2, date2);
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(snapshot);
        _dailySalesSnapshotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([snapshot, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, date2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.UpdateAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingSnapshot_DeletesAndReturnsOk()
    {
        var snapshot = CreateSnapshot(1);
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(snapshot);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _dailySalesSnapshotRepository.Verify(r => r.DeleteAsync(snapshot), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SnapshotNotFound_ReturnsNotFound()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DailySalesSnapshot?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _dailySalesSnapshotRepository.Verify(r => r.DeleteAsync(It.IsAny<DailySalesSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _dailySalesSnapshotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
