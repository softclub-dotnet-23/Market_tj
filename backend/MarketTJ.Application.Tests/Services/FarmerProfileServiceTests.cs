using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerProfileDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class FarmerProfileServiceTests
{
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IProductListingRepository> _productListingRepository = new();
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<FarmerProfileService>> _logger = new();
    private readonly FarmerProfileService _service;

    public FarmerProfileServiceTests()
    {
        _service = new FarmerProfileService(
            _farmerProfileRepository.Object,
            _userRepository.Object,
            _productListingRepository.Object,
            _reviewRepository.Object,
            _categoryRepository.Object,
            _currentUser.Object,
            _logger.Object);
        _currentUser.Setup(c => c.UserId).Returns(1);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Farmer, FullName = "Farmer", Email = "f@example.com", PhoneNumber = "+992900000000", PasswordHash = "hash" });
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _reviewRepository.Setup(r => r.GetRatingStatsByFarmerIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new Dictionary<int, (double, int)>());
        _productListingRepository.Setup(r => r.GetActiveListingStatsByFarmerIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new Dictionary<int, (int, List<int>)>());
        _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static FarmerProfile CreateProfile(int id = 1, int userId = 1) => new()
    {
        Id = id,
        UserId = userId,
        FarmName = "Test Farm",
        Region = "Хатлон",
        District = "Бохтар",
        Village = "Test Village",
        Address = "Test Address",
        VerificationStatus = FarmerVerificationStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateFarmerProfileDto ValidCreateDto(int userId = 1) => new()
    {
        UserId = userId,
        FarmName = "Test Farm",
        Region = "Хатлон",
        District = "Бохтар",
        Village = "Test Village",
        Address = "Test Address",
        VerificationStatus = FarmerVerificationStatus.Pending
    };

    private static UpdateFarmerProfileDto ValidUpdateDto(int id = 1, int userId = 1) => new()
    {
        Id = id,
        UserId = userId,
        FarmName = "Test Farm",
        Region = "Хатлон",
        District = "Бохтар",
        Village = "Test Village",
        Address = "Test Address",
        VerificationStatus = FarmerVerificationStatus.Pending
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ProfilesExist_ReturnsMappedDtos()
    {
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateProfile(1), CreateProfile(2, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetPublicCatalogAsync ----------

    [Fact]
    public async Task GetPublicCatalogAsync_MixedVerificationStatuses_ReturnsOnlyVerified()
    {
        var verified = CreateProfile(1, 1);
        verified.VerificationStatus = FarmerVerificationStatus.Verified;
        var pending = CreateProfile(2, 2);
        pending.VerificationStatus = FarmerVerificationStatus.Pending;
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([verified, pending]);

        var result = await _service.GetPublicCatalogAsync();

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Data!);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task GetPublicCatalogAsync_HasStats_ReturnsRatingReviewCountProductCountAndTags()
    {
        var verified = CreateProfile(1, 1);
        verified.VerificationStatus = FarmerVerificationStatus.Verified;
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([verified]);
        _reviewRepository.Setup(r => r.GetRatingStatsByFarmerIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, (double, int)> { [1] = (4.5, 3) });
        _productListingRepository.Setup(r => r.GetActiveListingStatsByFarmerIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, (int, List<int>)> { [1] = (2, [10, 20]) });
        _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new Category { Id = 10, Name = "Овощи", IsActive = true },
            new Category { Id = 20, Name = "Фрукты", IsActive = true },
        ]);

        var result = await _service.GetPublicCatalogAsync();

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Data!);
        Assert.Equal(4.5, dto.Rating);
        Assert.Equal(3, dto.ReviewCount);
        Assert.Equal(2, dto.ProductCount);
        Assert.Equal(["Овощи", "Фрукты"], dto.Tags);
    }

    [Fact]
    public async Task GetPublicCatalogAsync_NoVerifiedFarmers_ReturnsEmptyList()
    {
        var pending = CreateProfile(1, 1);
        pending.VerificationStatus = FarmerVerificationStatus.Pending;
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([pending]);

        var result = await _service.GetPublicCatalogAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetPublicCatalogAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetPublicCatalogAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var profile = CreateProfile(5);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(profile);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(profile.Id, result.Data!.Id);
        Assert.Equal(profile.FarmName, result.Data!.FarmName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsProfileAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyFarmName_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FarmName = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyRegion_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Region = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyDistrict_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.District = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyVillage_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Village = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyAddress_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Address = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidVerificationStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.VerificationStatus = (FarmerVerificationStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_VerifiedStatusWithoutAdminId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.VerificationStatus = FarmerVerificationStatus.Verified;
        dto.VerifiedAt = DateTime.UtcNow;
        dto.VerifiedByAdminId = null;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_VerifiedStatusWithoutVerifiedAt_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.VerificationStatus = FarmerVerificationStatus.Verified;
        dto.VerifiedByAdminId = 1;
        dto.VerifiedAt = null;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_VerifiedByAdminIdNotAdmin_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.VerificationStatus = FarmerVerificationStatus.Verified;
        dto.VerifiedAt = DateTime.UtcNow;
        dto.VerifiedByAdminId = 2;
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Role = UserRole.Farmer, FullName = "F", Email = "f@e.com", PhoneNumber = "1", PasswordHash = "h" });
        _userRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new User { Id = 2, Role = UserRole.Customer, FullName = "C", Email = "c@e.com", PhoneNumber = "2", PasswordHash = "h" });

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserAlreadyHasProfile_ReturnsConflict()
    {
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateProfile(1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.AddAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesProfileAndReturnsOk()
    {
        var profile = CreateProfile(1, 1);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 1));

        Assert.True(result.IsSuccess);
        _farmerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerProfile>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ProfileNotFound_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyFarmName_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.FarmName = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidVerificationStatus_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.VerificationStatus = (FarmerVerificationStatus)999;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UserNotFound_ReturnsNotFound()
    {
        var profile = CreateProfile(1, 1);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UserAlreadyHasAnotherProfile_ReturnsConflict()
    {
        var profile = CreateProfile(1, 1);
        var other = CreateProfile(2, 2);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);
        _farmerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([profile, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingProfile_DeletesAndReturnsOk()
    {
        var profile = CreateProfile(1);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _farmerProfileRepository.Verify(r => r.DeleteAsync(profile), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ProfileNotFound_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.DeleteAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- SetAutoReplyAsync ----------

    [Fact]
    public async Task SetAutoReplyAsync_Owner_EnablesAndReturnsOk()
    {
        var profile = CreateProfile(1, userId: 1);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);

        var result = await _service.SetAutoReplyAsync(1, true);

        Assert.True(result.IsSuccess);
        Assert.True(profile.AutoReplyToReviewsEnabled);
        _farmerProfileRepository.Verify(r => r.UpdateAsync(profile), Times.Once);
    }

    [Fact]
    public async Task SetAutoReplyAsync_Owner_DisablesAndReturnsOk()
    {
        var profile = CreateProfile(1, userId: 1);
        profile.AutoReplyToReviewsEnabled = true;
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);

        var result = await _service.SetAutoReplyAsync(1, false);

        Assert.True(result.IsSuccess);
        Assert.False(profile.AutoReplyToReviewsEnabled);
    }

    [Fact]
    public async Task SetAutoReplyAsync_ProfileNotFound_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.SetAutoReplyAsync(999, true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task SetAutoReplyAsync_NotOwner_ReturnsForbidden()
    {
        var profile = CreateProfile(1, userId: 2);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);

        var result = await _service.SetAutoReplyAsync(1, true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _farmerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerProfile>()), Times.Never);
    }

    [Fact]
    public async Task SetAutoReplyAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.SetAutoReplyAsync(1, true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
