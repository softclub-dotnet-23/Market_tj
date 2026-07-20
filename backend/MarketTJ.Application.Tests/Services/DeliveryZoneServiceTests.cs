using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliveryZoneDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class DeliveryZoneServiceTests
{
    private readonly Mock<IDeliveryZoneRepository> _deliveryZoneRepository = new();
    private readonly Mock<ILogger<DeliveryZoneService>> _logger = new();
    private readonly DeliveryZoneService _service;

    public DeliveryZoneServiceTests()
    {
        _service = new DeliveryZoneService(_deliveryZoneRepository.Object, _logger.Object);
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static DeliveryZone CreateZone(int id = 1, string region = "Хатлон", string district = "Бохтар") => new()
    {
        Id = id,
        Region = region,
        District = district,
        BasePrice = 10,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateDeliveryZoneDto ValidCreateDto(string region = "Хатлон", string district = "Бохтар") => new()
    {
        Region = region,
        District = district,
        BasePrice = 10,
        IsActive = true
    };

    private static UpdateDeliveryZoneDto ValidUpdateDto(int id = 1, string region = "Хатлон", string district = "Бохтар") => new()
    {
        Id = id,
        Region = region,
        District = district,
        BasePrice = 10,
        IsActive = true
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ZonesExist_ReturnsMappedDtos()
    {
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateZone(1), CreateZone(2, "Согд", "Худжанд")]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var zone = CreateZone(5);
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(zone);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(zone.Id, result.Data!.Id);
        Assert.Equal(zone.Region, result.Data!.Region);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DeliveryZone?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsZoneAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _deliveryZoneRepository.Verify(r => r.AddAsync(It.IsAny<DeliveryZone>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyRegion_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Region = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.AddAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyDistrict_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.District = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.AddAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeBasePrice_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.BasePrice = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.AddAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativePricePerKm_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.PricePerKm = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.AddAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateRegionDistrict_ReturnsConflict()
    {
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateZone(1, "Хатлон", "Бохтар")]);

        var result = await _service.CreateAsync(ValidCreateDto("Хатлон", "Бохтар"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.AddAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesZoneAndReturnsOk()
    {
        var zone = CreateZone(1);
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(zone);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _deliveryZoneRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliveryZone>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ZoneNotFound_ReturnsNotFound()
    {
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DeliveryZone?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyRegion_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Region = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateRegionDistrictOnAnotherZone_ReturnsConflict()
    {
        var zone = CreateZone(1, "Хатлон", "Бохтар");
        var other = CreateZone(2, "Согд", "Худжанд");
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(zone);
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([zone, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, "Согд", "Худжанд"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingZone_DeletesAndReturnsOk()
    {
        var zone = CreateZone(1);
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(zone);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _deliveryZoneRepository.Verify(r => r.DeleteAsync(zone), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ZoneNotFound_ReturnsNotFound()
    {
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DeliveryZone?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliveryZoneRepository.Verify(r => r.DeleteAsync(It.IsAny<DeliveryZone>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryZoneRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
