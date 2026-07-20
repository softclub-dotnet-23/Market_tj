using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FavoriteDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class FavoriteServiceTests
{
    private readonly Mock<IFavoriteRepository> _favoriteRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IProductListingRepository> _productListingRepository = new();
    private readonly Mock<ILogger<FavoriteService>> _logger = new();
    private readonly FavoriteService _service;

    public FavoriteServiceTests()
    {
        _service = new FavoriteService(_favoriteRepository.Object, _userRepository.Object, _productListingRepository.Object, _logger.Object);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new ProductListing
        {
            Id = id, FarmerProfileId = 1, ProductId = 1, Title = "Listing", RetailPricePerKg = 10,
            AvailableQuantity = 100, MinimumOrderQuantity = 1, QualityGrade = "A", Region = "Хатлон",
            District = "Бохтар", Address = "A", Status = ListingStatus.Active
        });
        _favoriteRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static Favorite CreateFavorite(int id = 1, int customerId = 1, int listingId = 1) => new()
    {
        Id = id,
        CustomerId = customerId,
        ProductListingId = listingId,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateFavoriteDto ValidCreateDto(int customerId = 1, int listingId = 1) => new()
    {
        CustomerId = customerId,
        ProductListingId = listingId
    };

    private static UpdateFavoriteDto ValidUpdateDto(int id = 1, int customerId = 1, int listingId = 1) => new()
    {
        Id = id,
        CustomerId = customerId,
        ProductListingId = listingId
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_FavoritesExist_ReturnsMappedDtos()
    {
        _favoriteRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateFavorite(1), CreateFavorite(2, 1, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _favoriteRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _favoriteRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var favorite = CreateFavorite(5);
        _favoriteRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(favorite);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(favorite.Id, result.Data!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _favoriteRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Favorite?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _favoriteRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsFavoriteAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroCustomerId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CustomerId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroProductListingId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ProductListingId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CustomerNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ListingNotFound_ReturnsNotFound()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicatePair_ReturnsConflict()
    {
        _favoriteRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateFavorite(1, 1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1, 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
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
    public async Task UpdateAsync_ValidData_UpdatesFavoriteAndReturnsOk()
    {
        var favorite = CreateFavorite(1);
        _favoriteRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(favorite);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _favoriteRepository.Verify(r => r.UpdateAsync(It.IsAny<Favorite>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_FavoriteNotFound_ReturnsNotFound()
    {
        _favoriteRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Favorite?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _favoriteRepository.Verify(r => r.UpdateAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CustomerNotFound_ReturnsNotFound()
    {
        var favorite = CreateFavorite(1);
        _favoriteRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(favorite);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _favoriteRepository.Verify(r => r.UpdateAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicatePairOnAnotherFavorite_ReturnsConflict()
    {
        var favorite = CreateFavorite(1, 1, 1);
        var other = CreateFavorite(2, 1, 2);
        _favoriteRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(favorite);
        _favoriteRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([favorite, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _favoriteRepository.Verify(r => r.UpdateAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _favoriteRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingFavorite_DeletesAndReturnsOk()
    {
        var favorite = CreateFavorite(1);
        _favoriteRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(favorite);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _favoriteRepository.Verify(r => r.DeleteAsync(favorite), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_FavoriteNotFound_ReturnsNotFound()
    {
        _favoriteRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Favorite?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _favoriteRepository.Verify(r => r.DeleteAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _favoriteRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
