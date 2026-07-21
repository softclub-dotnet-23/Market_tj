using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CommissionDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class CommissionServiceTests
{
    private readonly Mock<ICommissionRepository> _commissionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ILogger<CommissionService>> _logger = new();
    private readonly CommissionService _service;

    public CommissionServiceTests()
    {
        _service = new CommissionService(_commissionRepository.Object, _categoryRepository.Object, _logger.Object);
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Category { Id = id, Name = "Овощи", IsActive = true });
    }

    private static Commission CreateCommission(int id = 1, int? categoryId = null) => new()
    {
        Id = id,
        CategoryId = categoryId,
        Percentage = 5,
        EffectiveFrom = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateCommissionDto ValidCreateDto(int? categoryId = null) => new()
    {
        CategoryId = categoryId,
        Percentage = 5,
        EffectiveFrom = DateTime.UtcNow
    };

    private static UpdateCommissionDto ValidUpdateDto(int id = 1, int? categoryId = null) => new()
    {
        Id = id,
        CategoryId = categoryId,
        Percentage = 5,
        EffectiveFrom = DateTime.UtcNow
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_CommissionsExist_ReturnsMappedDtos()
    {
        _commissionRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateCommission(1), CreateCommission(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _commissionRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _commissionRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var commission = CreateCommission(5);
        _commissionRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(commission);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(commission.Id, result.Data!.Id);
        Assert.Equal(commission.Percentage, result.Data!.Percentage);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _commissionRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Commission?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _commissionRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsCommissionAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _commissionRepository.Verify(r => r.AddAsync(It.IsAny<Commission>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NegativePercentage_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Percentage = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _commissionRepository.Verify(r => r.AddAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PercentageAboveHundred_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Percentage = 101;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _commissionRepository.Verify(r => r.AddAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DefaultEffectiveFrom_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.EffectiveFrom = default;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _commissionRepository.Verify(r => r.AddAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EffectiveToBeforeEffectiveFrom_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.EffectiveFrom = DateTime.UtcNow;
        dto.EffectiveTo = DateTime.UtcNow.AddDays(-1);

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _commissionRepository.Verify(r => r.AddAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CategoryNotFound_ReturnsNotFound()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

        var result = await _service.CreateAsync(ValidCreateDto(categoryId: 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _commissionRepository.Verify(r => r.AddAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullCategoryId_IsAllowed()
    {
        var result = await _service.CreateAsync(ValidCreateDto(categoryId: null));

        Assert.True(result.IsSuccess);
        _commissionRepository.Verify(r => r.AddAsync(It.IsAny<Commission>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto(categoryId: 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesCommissionAndReturnsOk()
    {
        var commission = CreateCommission(1);
        _commissionRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(commission);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _commissionRepository.Verify(r => r.UpdateAsync(It.IsAny<Commission>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CommissionNotFound_ReturnsNotFound()
    {
        _commissionRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Commission?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _commissionRepository.Verify(r => r.UpdateAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_PercentageAboveHundred_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Percentage = 200;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _commissionRepository.Verify(r => r.UpdateAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CategoryNotFound_ReturnsNotFound()
    {
        var commission = CreateCommission(1);
        _commissionRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(commission);
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, categoryId: 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _commissionRepository.Verify(r => r.UpdateAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _commissionRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingCommission_DeletesAndReturnsOk()
    {
        var commission = CreateCommission(1);
        _commissionRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(commission);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _commissionRepository.Verify(r => r.DeleteAsync(commission), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CommissionNotFound_ReturnsNotFound()
    {
        _commissionRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Commission?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _commissionRepository.Verify(r => r.DeleteAsync(It.IsAny<Commission>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _commissionRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
