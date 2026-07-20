using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductListingDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class ProductListingServiceTests
{
    private readonly Mock<IProductListingRepository> _productListingRepository = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ILogger<ProductListingService>> _logger = new();
    private readonly ProductListingService _service;

    public ProductListingServiceTests()
    {
        _service = new ProductListingService(_productListingRepository.Object, _farmerProfileRepository.Object, _productRepository.Object, _logger.Object);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new FarmerProfile
        {
            Id = id,
            UserId = 1,
            FarmName = "Farm",
            Region = "Хатлон",
            District = "Бохтар",
            Village = "V",
            Address = "A",
            VerificationStatus = FarmerVerificationStatus.Verified
        });
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Product { Id = id, CategoryId = 1, Name = "Картофель", Unit = "кг", IsActive = true });
        _productListingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static ProductListing CreateListing(int id = 1, int farmerProfileId = 1, ListingStatus status = ListingStatus.Active) => new()
    {
        Id = id,
        FarmerProfileId = farmerProfileId,
        ProductId = 1,
        Title = "Свежий картофель",
        RetailPricePerKg = 10,
        AvailableQuantity = 100,
        MinimumOrderQuantity = 1,
        QualityGrade = "A",
        Region = "Хатлон",
        District = "Бохтар",
        Address = "Address",
        Status = status,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateProductListingDto ValidCreateDto() => new()
    {
        FarmerProfileId = 1,
        ProductId = 1,
        Title = "Свежий картофель",
        RetailPricePerKg = 10,
        AvailableQuantity = 100,
        MinimumOrderQuantity = 1,
        QualityGrade = "A",
        Region = "Хатлон",
        District = "Бохтар",
        Address = "Address",
        Status = ListingStatus.Draft
    };

    private static UpdateProductListingDto ValidUpdateDto(int id = 1) => new()
    {
        Id = id,
        FarmerProfileId = 1,
        ProductId = 1,
        Title = "Свежий картофель",
        RetailPricePerKg = 10,
        AvailableQuantity = 100,
        MinimumOrderQuantity = 1,
        QualityGrade = "A",
        Region = "Хатлон",
        District = "Бохтар",
        Address = "Address",
        Status = ListingStatus.Draft
    };

    // ---------- GetAllAsync (paginated) ----------

    [Fact]
    public async Task GetAllAsync_DataExists_ReturnsCorrectPageSlice()
    {
        _productListingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            CreateListing(1), CreateListing(2), CreateListing(3), CreateListing(4), CreateListing(5)
        ]);

        var result = await _service.GetAllAsync(1, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Items.Count);
        Assert.Equal(5, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_SecondPage_ReturnsRemainingItems()
    {
        _productListingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            CreateListing(1), CreateListing(2), CreateListing(3)
        ]);

        var result = await _service.GetAllAsync(2, 2);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyPage()
    {
        _productListingRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync(1, 10);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!.Items);
        Assert.Equal(0, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_ZeroPageNumber_ReturnsValidationError()
    {
        var result = await _service.GetAllAsync(0, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetAllAsync_NegativePageNumber_ReturnsValidationError()
    {
        var result = await _service.GetAllAsync(-1, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetAllAsync_ZeroPageSize_ReturnsValidationError()
    {
        var result = await _service.GetAllAsync(1, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productListingRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync(1, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var listing = CreateListing(5);
        _productListingRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(listing);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(listing.Id, result.Data!.Id);
        Assert.Equal(listing.Title, result.Data!.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsListingAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroFarmerProfileId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FarmerProfileId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroProductId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ProductId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Title = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_TitleTooShort_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Title = "Ab";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_TitleTooLong_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Title = new string('A', 151);

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DescriptionTooLong_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Description = new string('A', 2001);

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RetailPriceZero_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.RetailPricePerKg = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WholesalePriceHigherThanRetail_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.WholesalePricePerKg = 20;
        dto.WholesaleMinimumQuantity = 10;
        dto.RetailPricePerKg = 10;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WholesaleMinimumQuantityZero_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.WholesalePricePerKg = 5;
        dto.WholesaleMinimumQuantity = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WholesalePriceWithoutMinimumQuantity_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.WholesalePricePerKg = 5;
        dto.WholesaleMinimumQuantity = null;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WholesaleMinimumQuantityWithoutPrice_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.WholesalePricePerKg = null;
        dto.WholesaleMinimumQuantity = 10;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AvailableQuantityZero_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.AvailableQuantity = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_MinimumOrderQuantityZero_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.MinimumOrderQuantity = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_MinimumOrderQuantityExceedsAvailable_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.AvailableQuantity = 5;
        dto.MinimumOrderQuantity = 10;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyQualityGrade_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.QualityGrade = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyRegion_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Region = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyDistrict_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.District = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyAddress_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Address = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = (ListingStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FarmerProfileNotFound_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ProductNotFound_ReturnsNotFound()
    {
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ActiveStatusWithUnverifiedFarmer_ReturnsValidationError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 1, FarmName = "Farm", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A",
            VerificationStatus = FarmerVerificationStatus.Pending
        });
        var dto = ValidCreateDto();
        dto.Status = ListingStatus.Active;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.AddAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesListingAndReturnsOk()
    {
        var listing = CreateListing(1);
        _productListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(listing);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _productListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductListing>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ListingNotFound_ReturnsNotFound()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyTitle_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Title = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AvailableQuantityZero_IsAllowedByValidator()
    {
        var listing = CreateListing(1);
        _productListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(listing);
        var dto = ValidUpdateDto(1);
        dto.AvailableQuantity = 0;
        dto.MinimumOrderQuantity = 1;
        dto.Status = ListingStatus.Active;

        var result = await _service.UpdateAsync(1, dto);

        Assert.True(result.IsSuccess);
        _productListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductListing>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AvailableQuantityZeroWithActiveStatus_TransitionsToOutOfStock()
    {
        var listing = CreateListing(1);
        _productListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(listing);
        var dto = ValidUpdateDto(1);
        dto.AvailableQuantity = 0;
        dto.MinimumOrderQuantity = 1;
        dto.Status = ListingStatus.Active;

        await _service.UpdateAsync(1, dto);

        Assert.Equal(ListingStatus.OutOfStock, listing.Status);
    }

    [Fact]
    public async Task UpdateAsync_FarmerProfileNotFound_ReturnsNotFound()
    {
        var listing = CreateListing(1);
        _productListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(listing);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ProductNotFound_ReturnsNotFound()
    {
        var listing = CreateListing(1);
        _productListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(listing);
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ActiveStatusWithUnverifiedFarmer_ReturnsValidationError()
    {
        var listing = CreateListing(1);
        _productListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(listing);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 1, FarmName = "Farm", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A",
            VerificationStatus = FarmerVerificationStatus.Rejected
        });
        var dto = ValidUpdateDto(1);
        dto.Status = ListingStatus.Active;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _productListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingListing_SoftDeletesAndReturnsOk()
    {
        var listing = CreateListing(1);
        _productListingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(listing);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(listing.IsDeleted);
        Assert.NotNull(listing.DeletedAt);
        _productListingRepository.Verify(r => r.UpdateAsync(listing), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ListingNotFound_ReturnsNotFound()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _productListingRepository.Verify(r => r.UpdateAsync(It.IsAny<ProductListing>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
