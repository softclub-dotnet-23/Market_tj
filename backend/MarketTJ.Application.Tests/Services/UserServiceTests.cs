using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.UserDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ILogger<UserService>> _logger = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_userRepository.Object, _auditLogService.Object, _logger.Object);
    }

    private static User CreateUser(int id = 1, string email = "user@example.com", string phone = "+992900000000") => new()
    {
        Id = id,
        FullName = "Test User",
        Email = email,
        PhoneNumber = phone,
        PasswordHash = "hashedpassword",
        Role = UserRole.Customer,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateUserDto ValidCreateDto() => new()
    {
        FullName = "Test User",
        Email = "user@example.com",
        PhoneNumber = "+992900000000",
        PasswordHash = "hashedpassword",
        Role = UserRole.Customer,
        IsActive = true
    };

    private static UpdateUserDto ValidUpdateDto(int id = 1) => new()
    {
        Id = id,
        FullName = "Test User",
        Email = "user@example.com",
        PhoneNumber = "+992900000000",
        PasswordHash = "hashedpassword",
        Role = UserRole.Customer,
        IsActive = true
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_UsersExist_ReturnsMappedDtos()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateUser(1), CreateUser(2, "user2@example.com", "+992900000001")]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var user = CreateUser(5);
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Data!.Id);
        Assert.Equal(user.Email, result.Data!.Email);
        Assert.Equal(user.FullName, result.Data!.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsUserAndReturnsOk()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyFullName_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FullName = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FullNameTooShort_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FullName = "Ab";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FullNameTooLong_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FullName = new string('A', 101);

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyEmail_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Email = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidEmailFormat_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Email = "not-an-email";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyPhoneNumber_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.PhoneNumber = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyPasswordHash_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.PasswordHash = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PasswordTooShort_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.PasswordHash = "12345";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidRole_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Role = (UserRole)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ReturnsConflict()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateUser(1, "user@example.com", "+992900000099")]);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicatePhoneNumber_ReturnsConflict()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateUser(1, "other@example.com", "+992900000000")]);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesUserAndReturnsOk()
    {
        var user = CreateUser(1);
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([user]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UserNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyFullName_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.FullName = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidEmailFormat_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Email = "not-an-email";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyPhoneNumber_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.PhoneNumber = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_PasswordTooShort_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.PasswordHash = "123";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidRole_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Role = (UserRole)999;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateEmailOnAnotherUser_ReturnsConflict()
    {
        var user = CreateUser(1, "user@example.com", "+992900000000");
        var other = CreateUser(2, "user@example.com", "+992900000099");
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([user, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicatePhoneOnAnotherUser_ReturnsConflict()
    {
        var user = CreateUser(1, "user@example.com", "+992900000000");
        var other = CreateUser(2, "other@example.com", "+992900000000");
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([user, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingUser_SoftDeletesAndReturnsOk()
    {
        var user = CreateUser(1);
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsDeleted);
        Assert.NotNull(user.DeletedAt);
        _userRepository.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
