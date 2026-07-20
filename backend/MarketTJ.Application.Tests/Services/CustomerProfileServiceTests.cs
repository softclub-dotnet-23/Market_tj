using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CustomerProfileDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class CustomerProfileServiceTests
{
    private readonly Mock<ICustomerProfileRepository> _customerProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<CustomerProfileService>> _logger = new();
    private readonly CustomerProfileService _service;

    public CustomerProfileServiceTests()
    {
        _service = new CustomerProfileService(_customerProfileRepository.Object, _userRepository.Object, _logger.Object);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Customer, FullName = "Customer", Email = "c@example.com", PhoneNumber = "+992900000000", PasswordHash = "hash" });
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static CustomerProfile CreateProfile(int id = 1, int userId = 1) => new()
    {
        Id = id,
        UserId = userId,
        CustomerType = CustomerType.Retail,
        Region = "Хатлон",
        District = "Бохтар",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateCustomerProfileDto ValidCreateDto(int userId = 1) => new()
    {
        UserId = userId,
        CustomerType = CustomerType.Retail,
        Region = "Хатлон",
        District = "Бохтар"
    };

    private static UpdateCustomerProfileDto ValidUpdateDto(int id = 1, int userId = 1) => new()
    {
        Id = id,
        UserId = userId,
        CustomerType = CustomerType.Retail,
        Region = "Хатлон",
        District = "Бохтар"
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ProfilesExist_ReturnsMappedDtos()
    {
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateProfile(1), CreateProfile(2, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var profile = CreateProfile(5);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(profile);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(profile.Id, result.Data!.Id);
        Assert.Equal(profile.Region, result.Data!.Region);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CustomerProfile?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

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
        _customerProfileRepository.Verify(r => r.AddAsync(It.IsAny<CustomerProfile>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyRegion_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Region = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _customerProfileRepository.Verify(r => r.AddAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyDistrict_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.District = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _customerProfileRepository.Verify(r => r.AddAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidCustomerType_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CustomerType = (CustomerType)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _customerProfileRepository.Verify(r => r.AddAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _customerProfileRepository.Verify(r => r.AddAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserAlreadyHasProfile_ReturnsConflict()
    {
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateProfile(1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _customerProfileRepository.Verify(r => r.AddAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesProfileAndReturnsOk()
    {
        var profile = CreateProfile(1, 1);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 1));

        Assert.True(result.IsSuccess);
        _customerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerProfile>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ProfileNotFound_ReturnsNotFound()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CustomerProfile?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _customerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyRegion_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Region = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _customerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidCustomerType_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.CustomerType = (CustomerType)999;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _customerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UserNotFound_ReturnsNotFound()
    {
        var profile = CreateProfile(1, 1);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _customerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UserAlreadyHasAnotherProfile_ReturnsConflict()
    {
        var profile = CreateProfile(1, 1);
        var other = CreateProfile(2, 2);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([profile, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _customerProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingProfile_DeletesAndReturnsOk()
    {
        var profile = CreateProfile(1);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(profile);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _customerProfileRepository.Verify(r => r.DeleteAsync(profile), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ProfileNotFound_ReturnsNotFound()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CustomerProfile?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _customerProfileRepository.Verify(r => r.DeleteAsync(It.IsAny<CustomerProfile>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
