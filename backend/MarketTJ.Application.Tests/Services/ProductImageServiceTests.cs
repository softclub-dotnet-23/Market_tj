using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductImageDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class ProductImageServiceTests
{
    private readonly Mock<IProductImageRepository> _productImageRepository = new();
    private readonly Mock<IProductListingRepository> _productListingRepository = new();
    private readonly Mock<ILogger<ProductImageService>> _logger = new();
    private readonly ProductImageService _service;

    public ProductImageServiceTests()
    {
        _service = new ProductImageService(_productImageRepository.Object, _productListingRepository.Object, _logger.Object);
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new ProductListing
        {
            Id = id, FarmerProfileId = 1, ProductId = 1, Title = "Listing", RetailPricePerKg = 10,
            AvailableQuantity = 10, MinimumOrderQuantity = 1, QualityGrade = "A", Region = "Хатлон",
            District = "Бохтар", Address = "A", Status = ListingStatus.Active
        });
        _productImageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static ProductImage CreateImage(int id = 1, int listingId = 1) => new()
    {
        Id = id,
        ProductListingId = listingId,
        ImageUrl = "photo.jpg",
        IsMain = false,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateProductImageDto ValidCreateDto(int listingId = 1) => new()
    {
        ProductListingId = listingId,
        ImageUrl = "photo.jpg",
        IsMain = false
    };

    private static UpdateProductImageDto ValidUpdateDto(int id = 1, int listingId = 1) => new()
    {
        Id = id,
        ProductListingId = listingId,
        ImageUrl = "photo.jpg",
        IsMain = false
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ImagesExist_ReturnsMappedDtos()
    {
        _productImageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateImage(1), CreateImage(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _productImageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productImageRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var image = CreateImage(5);
        _productImageRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(image);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(image.Id, result.Data!.Id);
        Assert.Equal(image.ImageUrl, result.Data!.ImageUrl);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _productImageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductImage?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productImageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsImageAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _productImageRepository.Verify(r => r.AddAsync(It.IsAny<ProductImage>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroProductListingId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ProductListingId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productImageRepository.Verify(r => r.AddAsync(It.IsAny<ProductImage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyImageUrl_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ImageUrl = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productImageRepository.Verify(r => r.AddAsync(It.IsAny<ProductImage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DisallowedFileExtension_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ImageUrl = "photo.gif";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productImageRepository.Verify(r => r.AddAsync(It.IsAny<ProductImage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ListingNotFound_ReturnsNotFound()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productImageRepository.Verify(r => r.AddAsync(It.IsAny<ProductImage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ListingAlreadyHasFiveImages_ReturnsValidationError()
    {
        _productImageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            CreateImage(1, 1), CreateImage(2, 1), CreateImage(3, 1), CreateImage(4, 1), CreateImage(5, 1)
        ]);

        var result = await _service.CreateAsync(ValidCreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productImageRepository.Verify(r => r.AddAsync(It.IsAny<ProductImage>()), Times.Never);
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
    public async Task UpdateAsync_ValidData_UpdatesImageAndReturnsOk()
    {
        var image = CreateImage(1);
        _productImageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(image);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _productImageRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductImage>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ImageNotFound_ReturnsNotFound()
    {
        _productImageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductImage?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productImageRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductImage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DisallowedFileExtension_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.ImageUrl = "photo.bmp";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productImageRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductImage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ListingNotFound_ReturnsNotFound()
    {
        var image = CreateImage(1);
        _productImageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(image);
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productImageRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductImage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productImageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingImage_DeletesAndReturnsOk()
    {
        var image = CreateImage(1);
        _productImageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(image);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _productImageRepository.Verify(r => r.DeleteAsync(image), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ImageNotFound_ReturnsNotFound()
    {
        _productImageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductImage?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productImageRepository.Verify(r => r.DeleteAsync(It.IsAny<ProductImage>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productImageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
