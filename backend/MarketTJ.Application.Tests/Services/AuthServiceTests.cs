using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_userRepository.Object, _refreshTokenRepository.Object, _tokenService.Object, _logger.Object);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("access-token");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
        _tokenService.Setup(t => t.AccessTokenExpiryMinutes).Returns(60);
        _tokenService.Setup(t => t.RefreshTokenExpiryDays).Returns(30);
    }

    private static User CreateUser(int id = 1, string email = "user@example.com", string password = "Password1", UserRole role = UserRole.Customer, bool isActive = true) => new()
    {
        Id = id,
        FullName = "Test User",
        Email = email,
        PhoneNumber = "+992900000000",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Role = role,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static RegisterRequestDto ValidRegisterDto() => new()
    {
        FullName = "New User",
        Email = "new@example.com",
        PhoneNumber = "+992900000001",
        Password = "Password1",
        Role = UserRole.Customer
    };

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_ValidData_AddsUserAndIssuesTokens()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => u.Id = 42).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(ValidRegisterDto());

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Data!.Token);
        Assert.Equal("refresh-token", result.Data.RefreshToken);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsConflict()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(CreateUser());

        var result = await _service.RegisterAsync(ValidRegisterDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_AdminRole_ReturnsValidationError()
    {
        var dto = ValidRegisterDto();
        dto.Role = UserRole.Admin;

        var result = await _service.RegisterAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShortPassword_ReturnsValidationError()
    {
        var dto = ValidRegisterDto();
        dto.Password = "123";

        var result = await _service.RegisterAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task RegisterAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.RegisterAsync(ValidRegisterDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- LoginAsync ----------

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        var user = CreateUser(1, "user@example.com", "Password1");
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequestDto { Email = "user@example.com", Password = "Password1" });

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Data!.Token);
        Assert.Equal(user.Id, result.Data.UserId);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorized()
    {
        var user = CreateUser(1, "user@example.com", "Password1");
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequestDto { Email = "user@example.com", Password = "WrongPassword" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsUnauthorized()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _service.LoginAsync(new LoginRequestDto { Email = "missing@example.com", Password = "Password1" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsUnauthorized()
    {
        var user = CreateUser(1, "user@example.com", "Password1", isActive: false);
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequestDto { Email = "user@example.com", Password = "Password1" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task LoginAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.LoginAsync(new LoginRequestDto { Email = "user@example.com", Password = "Password1" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- RefreshTokenAsync ----------

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RevokesOldAndIssuesNewPair()
    {
        var user = CreateUser(7);
        var existing = new RefreshToken { Id = 1, UserId = 7, Token = "old-refresh", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync("old-refresh")).ReturnsAsync(existing);
        _userRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "old-refresh" });

        Assert.True(result.IsSuccess);
        Assert.True(existing.IsRevoked);
        _refreshTokenRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenNotFound_ReturnsUnauthorized()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "missing" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenAlreadyRevoked_ReturnsUnauthorized()
    {
        var existing = new RefreshToken { Id = 1, UserId = 1, Token = "revoked", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = true };
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync("revoked")).ReturnsAsync(existing);

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "revoked" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenExpired_ReturnsUnauthorized()
    {
        var existing = new RefreshToken { Id = 1, UserId = 1, Token = "expired", ExpiresAt = DateTime.UtcNow.AddDays(-1), IsRevoked = false };
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync("expired")).ReturnsAsync(existing);

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "expired" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RefreshTokenAsync_UserInactive_ReturnsUnauthorized()
    {
        var user = CreateUser(7, isActive: false);
        var existing = new RefreshToken { Id = 1, UserId = 7, Token = "tok", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync("tok")).ReturnsAsync(existing);
        _userRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "tok" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RefreshTokenAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "tok" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- LogoutAsync ----------

    [Fact]
    public async Task LogoutAsync_ExistingToken_RevokesAndReturnsOk()
    {
        var existing = new RefreshToken { Id = 1, UserId = 1, Token = "tok", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync("tok")).ReturnsAsync(existing);

        var result = await _service.LogoutAsync(new RefreshTokenRequestDto { RefreshToken = "tok" });

        Assert.True(result.IsSuccess);
        Assert.True(existing.IsRevoked);
        _refreshTokenRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_TokenNotFound_IsIdempotentAndReturnsOk()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

        var result = await _service.LogoutAsync(new RefreshTokenRequestDto { RefreshToken = "missing" });

        Assert.True(result.IsSuccess);
        _refreshTokenRepository.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.LogoutAsync(new RefreshTokenRequestDto { RefreshToken = "tok" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
