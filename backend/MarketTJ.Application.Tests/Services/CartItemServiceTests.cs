using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CartItemDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class CartItemServiceTests
{
    private readonly Mock<ICartItemRepository> _cartItemRepository = new();
    private readonly Mock<ICustomerProfileRepository> _customerProfileRepository = new();
    private readonly Mock<IProductListingRepository> _productListingRepository = new();
    private readonly Mock<ILogger<CartItemService>> _logger = new();
    private readonly CartItemService _service;

    public CartItemServiceTests()
    {
        _service = new CartItemService(_cartItemRepository.Object, _customerProfileRepository.Object, _productListingRepository.Object, _logger.Object);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CustomerProfile { Id = id, UserId = 1, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new ProductListing
        {
            Id = id, FarmerProfileId = 1, ProductId = 1, Title = "Listing", RetailPricePerKg = 10,
            AvailableQuantity = 100, MinimumOrderQuantity = 1, QualityGrade = "A", Region = "Хатлон",
            District = "Бохтар", Address = "A", Status = ListingStatus.Active
        });
        _cartItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static CartItem CreateItem(int id = 1, int customerId = 1, int listingId = 1, decimal quantity = 5) => new()
    {
        Id = id,
        CustomerId = customerId,
        ProductListingId = listingId,
        Quantity = quantity,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateCartItemDto ValidCreateDto(int customerId = 1, int listingId = 1, decimal quantity = 5) => new()
    {
        CustomerId = customerId,
        ProductListingId = listingId,
        Quantity = quantity
    };

    private static UpdateCartItemDto ValidUpdateDto(int id = 1, int customerId = 1, int listingId = 1, decimal quantity = 5) => new()
    {
        Id = id,
        CustomerId = customerId,
        ProductListingId = listingId,
        Quantity = quantity
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ItemsExist_ReturnsMappedDtos()
    {
        _cartItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateItem(1), CreateItem(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _cartItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _cartItemRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var item = CreateItem(5);
        _cartItemRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(item);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(item.Id, result.Data!.Id);
        Assert.Equal(item.Quantity, result.Data!.Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _cartItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CartItem?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _cartItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsItemAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroCustomerId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CustomerId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroProductListingId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ProductListingId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroQuantity_ReturnsValidationError()
    {
        var dto = ValidCreateDto(quantity: 0);

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CustomerNotFound_ReturnsNotFound()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CustomerProfile?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ListingNotFound_ReturnsNotFound()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_QuantityBelowMinimumOrder_ReturnsValidationError()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new ProductListing
        {
            Id = 1, FarmerProfileId = 1, ProductId = 1, Title = "Listing", RetailPricePerKg = 10,
            AvailableQuantity = 100, MinimumOrderQuantity = 10, QualityGrade = "A", Region = "Хатлон",
            District = "Бохтар", Address = "A", Status = ListingStatus.Active
        });

        var result = await _service.CreateAsync(ValidCreateDto(quantity: 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_QuantityExceedsAvailable_ReturnsValidationError()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new ProductListing
        {
            Id = 1, FarmerProfileId = 1, ProductId = 1, Title = "Listing", RetailPricePerKg = 10,
            AvailableQuantity = 3, MinimumOrderQuantity = 1, QualityGrade = "A", Region = "Хатлон",
            District = "Бохтар", Address = "A", Status = ListingStatus.Active
        });

        var result = await _service.CreateAsync(ValidCreateDto(quantity: 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateListingInCart_ReturnsConflict()
    {
        _cartItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateItem(1, 1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1, 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _cartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesItemAndReturnsOk()
    {
        var item = CreateItem(1);
        _cartItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _cartItemRepository.Verify(r => r.UpdateAsync(It.IsAny<CartItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ItemNotFound_ReturnsNotFound()
    {
        _cartItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CartItem?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _cartItemRepository.Verify(r => r.UpdateAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ZeroQuantity_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1, quantity: 0);

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cartItemRepository.Verify(r => r.UpdateAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_QuantityExceedsAvailable_ReturnsValidationError()
    {
        var item = CreateItem(1);
        _cartItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new ProductListing
        {
            Id = 1, FarmerProfileId = 1, ProductId = 1, Title = "Listing", RetailPricePerKg = 10,
            AvailableQuantity = 3, MinimumOrderQuantity = 1, QualityGrade = "A", Region = "Хатлон",
            District = "Бохтар", Address = "A", Status = ListingStatus.Active
        });

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, quantity: 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cartItemRepository.Verify(r => r.UpdateAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CustomerNotFound_ReturnsNotFound()
    {
        var item = CreateItem(1);
        _cartItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CustomerProfile?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _cartItemRepository.Verify(r => r.UpdateAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ListingNotFound_ReturnsNotFound()
    {
        var item = CreateItem(1);
        _cartItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _cartItemRepository.Verify(r => r.UpdateAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateListingOnAnotherItem_ReturnsConflict()
    {
        var item = CreateItem(1, 1, 1);
        var other = CreateItem(2, 1, 2);
        _cartItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _cartItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([item, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _cartItemRepository.Verify(r => r.UpdateAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _cartItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingItem_DeletesAndReturnsOk()
    {
        var item = CreateItem(1);
        _cartItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _cartItemRepository.Verify(r => r.DeleteAsync(item), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ItemNotFound_ReturnsNotFound()
    {
        _cartItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CartItem?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _cartItemRepository.Verify(r => r.DeleteAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _cartItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
