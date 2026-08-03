using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailVerificationService> _emailVerificationService = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_userRepository.Object, _refreshTokenRepository.Object, _tokenService.Object, _emailVerificationService.Object, _configuration.Object, _logger.Object);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("access-token");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
        _tokenService.Setup(t => t.AccessTokenExpiryMinutes).Returns(60);
        _tokenService.Setup(t => t.RefreshTokenExpiryDays).Returns(30);
        _emailVerificationService.Setup(e => e.IsEmailVerifiedAsync(It.IsAny<string>())).ReturnsAsync(true);
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
    public async Task RegisterAsync_EmailNotVerified_ReturnsValidationError()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _emailVerificationService.Setup(e => e.IsEmailVerifiedAsync(It.IsAny<string>())).ReturnsAsync(false);

        var result = await _service.RegisterAsync(ValidRegisterDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_VerificationNotRequired_SkipsCheckAndRegisters()
    {
        _configuration.Setup(c => c["EmailVerification:RequireVerification"]).Returns("false");
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => u.Id = 42).Returns(Task.CompletedTask);
        _emailVerificationService.Setup(e => e.IsEmailVerifiedAsync(It.IsAny<string>())).ReturnsAsync(false);

        var result = await _service.RegisterAsync(ValidRegisterDto());

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _emailVerificationService.Verify(e => e.IsEmailVerifiedAsync(It.IsAny<string>()), Times.Never);
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
    public async Task RegisterAsync_CourierRole_AddsUserAndIssuesTokens()
    {
        var dto = ValidRegisterDto();
        dto.Role = UserRole.Courier;
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => u.Id = 42).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(dto);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u => u.Role == UserRole.Courier)), Times.Once);
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

    // ---------- SendRegistrationVerificationCodeAsync ----------

    [Fact]
    public async Task SendRegistrationVerificationCodeAsync_NewEmail_SendsCode()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("new@example.com")).ReturnsAsync((User?)null);
        _emailVerificationService.Setup(e => e.SendCodeAsync("new@example.com")).ReturnsAsync(Result<string>.Ok("Код отправлен"));

        var result = await _service.SendRegistrationVerificationCodeAsync("new@example.com");

        Assert.True(result.IsSuccess);
        _emailVerificationService.Verify(e => e.SendCodeAsync("new@example.com"), Times.Once);
    }

    [Fact]
    public async Task SendRegistrationVerificationCodeAsync_EmailAlreadyExists_ReturnsConflictWithoutSendingCode()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(CreateUser(email: "user@example.com"));

        var result = await _service.SendRegistrationVerificationCodeAsync("user@example.com");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _emailVerificationService.Verify(e => e.SendCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendRegistrationVerificationCodeAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.SendRegistrationVerificationCodeAsync("new@example.com");

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

    // ---------- ForgotPasswordAsync ----------

    [Fact]
    public async Task ForgotPasswordAsync_ExistingActiveUser_SendsCodeAndReturnsGenericMessage()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(CreateUser(email: "user@example.com"));

        var result = await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "user@example.com" });

        Assert.True(result.IsSuccess);
        _emailVerificationService.Verify(e => e.SendCodeAsync("user@example.com"), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_NoSuchUser_DoesNotSendCodeButReturnsSameGenericMessage()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "nobody@example.com" });

        Assert.True(result.IsSuccess);
        _emailVerificationService.Verify(e => e.SendCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_InactiveUser_DoesNotSendCode()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(CreateUser(email: "user@example.com", isActive: false));

        var result = await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "user@example.com" });

        Assert.True(result.IsSuccess);
        _emailVerificationService.Verify(e => e.SendCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "user@example.com" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- ResetPasswordAsync ----------

    private static ResetPasswordRequestDto ValidResetDto() => new() { Email = "user@example.com", Code = "123456", NewPassword = "NewPassword1" };

    [Fact]
    public async Task ResetPasswordAsync_ValidCode_UpdatesPasswordHash()
    {
        var user = CreateUser(email: "user@example.com", password: "OldPassword1");
        _emailVerificationService.Setup(e => e.VerifyCodeAsync("user@example.com", "123456")).ReturnsAsync(Result<string>.Ok("Email подтверждён"));
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var result = await _service.ResetPasswordAsync(ValidResetDto());

        Assert.True(result.IsSuccess);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword1", user.PasswordHash));
        _userRepository.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShortPassword_ReturnsValidationError()
    {
        var dto = ValidResetDto();
        dto.NewPassword = "123";

        var result = await _service.ResetPasswordAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _emailVerificationService.Verify(e => e.VerifyCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidCode_ReturnsFailureFromVerification()
    {
        _emailVerificationService.Setup(e => e.VerifyCodeAsync("user@example.com", "123456")).ReturnsAsync(Result<string>.Fail("Неверный код", ErrorType.Validation));

        var result = await _service.ResetPasswordAsync(ValidResetDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_UserNotFound_ReturnsNotFound()
    {
        _emailVerificationService.Setup(e => e.VerifyCodeAsync("user@example.com", "123456")).ReturnsAsync(Result<string>.Ok("Email подтверждён"));
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync((User?)null);

        var result = await _service.ResetPasswordAsync(ValidResetDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ResetPasswordAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _emailVerificationService.Setup(e => e.VerifyCodeAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.ResetPasswordAsync(ValidResetDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
