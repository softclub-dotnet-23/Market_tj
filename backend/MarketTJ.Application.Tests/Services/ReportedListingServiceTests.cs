using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ReportedListingDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class ReportedListingServiceTests
{
    private readonly Mock<IReportedListingRepository> _reportedListingRepository = new();
    private readonly Mock<IProductListingRepository> _productListingRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ILogger<ReportedListingService>> _logger = new();
    private readonly ReportedListingService _service;

    public ReportedListingServiceTests()
    {
        _service = new ReportedListingService(_reportedListingRepository.Object, _productListingRepository.Object, _userRepository.Object, _auditLogService.Object, _logger.Object);
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new ProductListing
        {
            Id = id, FarmerProfileId = 1, ProductId = 1, Title = "Listing", RetailPricePerKg = 10,
            AvailableQuantity = 100, MinimumOrderQuantity = 1, QualityGrade = "A", Region = "Хатлон",
            District = "Бохтар", Address = "A", Status = ListingStatus.Active
        });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Admin, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
    }

    private static ReportedListing CreateReport(int id = 1, int listingId = 1) => new()
    {
        Id = id,
        ProductListingId = listingId,
        ReportedByUserId = 1,
        Reason = ReportReason.Fraud,
        Status = ReportStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateReportedListingDto ValidCreateDto(int listingId = 1) => new()
    {
        ProductListingId = listingId,
        ReportedByUserId = 1,
        Reason = ReportReason.Fraud,
        Status = ReportStatus.Pending
    };

    private static UpdateReportedListingDto ValidUpdateDto(int id = 1, int listingId = 1) => new()
    {
        Id = id,
        ProductListingId = listingId,
        ReportedByUserId = 1,
        Reason = ReportReason.Fraud,
        Status = ReportStatus.Pending
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ReportsExist_ReturnsMappedDtos()
    {
        _reportedListingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateReport(1), CreateReport(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _reportedListingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _reportedListingRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var report = CreateReport(5);
        _reportedListingRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(report);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(report.Id, result.Data!.Id);
        Assert.Equal(report.Reason, result.Data!.Reason);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _reportedListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ReportedListing?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _reportedListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsReportAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _reportedListingRepository.Verify(r => r.AddAsync(It.IsAny<ReportedListing>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroProductListingId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ProductListingId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reportedListingRepository.Verify(r => r.AddAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroReportedByUserId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ReportedByUserId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reportedListingRepository.Verify(r => r.AddAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidReason_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Reason = (ReportReason)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reportedListingRepository.Verify(r => r.AddAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = (ReportStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reportedListingRepository.Verify(r => r.AddAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ListingNotFound_ReturnsNotFound()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reportedListingRepository.Verify(r => r.AddAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReportedByUserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reportedListingRepository.Verify(r => r.AddAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReviewedByAdminNotFound_ReturnsNotFound()
    {
        _userRepository.SetupSequence(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" })
            .ReturnsAsync((User?)null);
        var dto = ValidCreateDto();
        dto.ReviewedByAdminId = 5;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reportedListingRepository.Verify(r => r.AddAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesReportAndReturnsOk()
    {
        var report = CreateReport(1);
        _reportedListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _reportedListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ReportedListing>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReportNotFound_ReturnsNotFound()
    {
        _reportedListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ReportedListing?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reportedListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ListingNotFound_ReturnsNotFound()
    {
        var report = CreateReport(1);
        _reportedListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reportedListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _reportedListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingReport_DeletesAndReturnsOk()
    {
        var report = CreateReport(1);
        _reportedListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _reportedListingRepository.Verify(r => r.DeleteAsync(report), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReportNotFound_ReturnsNotFound()
    {
        _reportedListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ReportedListing?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reportedListingRepository.Verify(r => r.DeleteAsync(It.IsAny<ReportedListing>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _reportedListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
