using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.WalletDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class WalletPinServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<WalletPinService>> _logger = new();
    private readonly WalletPinService _service;

    public WalletPinServiceTests()
    {
        _service = new WalletPinService(_userRepository.Object, _currentUser.Object, _logger.Object);
        _currentUser.Setup(c => c.UserId).Returns(42);
    }

    private static User NewUser(string? pinHash = null, int failedAttempts = 0, DateTime? lockedUntil = null) => new()
    {
        Id = 42,
        FullName = "Alice Smith",
        Email = "alice@example.com",
        PhoneNumber = "+992900000000",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1"),
        WalletPinHash = pinHash,
        WalletPinFailedAttempts = failedAttempts,
        WalletPinLockedUntil = lockedUntil,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetStatusAsync_NoPinSet_ReturnsFalse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(NewUser());

        var result = await _service.GetStatusAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsSet);
    }

    [Fact]
    public async Task GetStatusAsync_PinSet_ReturnsTrue()
    {
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(NewUser(BCrypt.Net.BCrypt.HashPassword("1234")));

        var result = await _service.GetStatusAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsSet);
    }

    [Fact]
    public async Task SetPinAsync_CorrectPasswordValidPin_SetsHash()
    {
        var user = NewUser();
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.SetPinAsync(new SetWalletPinDto { Pin = "1234", Password = "CorrectPassword1" });

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.WalletPinHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("1234", user.WalletPinHash));
        _userRepository.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task SetPinAsync_WrongPassword_Fails()
    {
        var user = NewUser();
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.SetPinAsync(new SetWalletPinDto { Pin = "1234", Password = "WrongPassword" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Null(user.WalletPinHash);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12ab")]
    public async Task SetPinAsync_InvalidPinFormat_Fails(string pin)
    {
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(NewUser());

        var result = await _service.SetPinAsync(new SetWalletPinDto { Pin = pin, Password = "CorrectPassword1" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task SetPinAsync_PinAlreadySet_Fails()
    {
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(NewUser(BCrypt.Net.BCrypt.HashPassword("1234")));

        var result = await _service.SetPinAsync(new SetWalletPinDto { Pin = "5678", Password = "CorrectPassword1" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task VerifyPinAsync_NoPinSet_Fails()
    {
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(NewUser());

        var result = await _service.VerifyPinAsync(new VerifyWalletPinDto { Pin = "1234" });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task VerifyPinAsync_CorrectPin_SucceedsAndResetsAttempts()
    {
        var user = NewUser(BCrypt.Net.BCrypt.HashPassword("1234"), failedAttempts: 2);
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.VerifyPinAsync(new VerifyWalletPinDto { Pin = "1234" });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, user.WalletPinFailedAttempts);
        Assert.Null(user.WalletPinLockedUntil);
    }

    [Fact]
    public async Task VerifyPinAsync_WrongPin_IncrementsAttemptsAndReportsRemaining()
    {
        var user = NewUser(BCrypt.Net.BCrypt.HashPassword("1234"));
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.VerifyPinAsync(new VerifyWalletPinDto { Pin = "0000" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(1, user.WalletPinFailedAttempts);
        Assert.Contains("4", result.Error);
    }

    [Fact]
    public async Task VerifyPinAsync_FifthWrongAttempt_LocksAccount()
    {
        var user = NewUser(BCrypt.Net.BCrypt.HashPassword("1234"), failedAttempts: 4);
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.VerifyPinAsync(new VerifyWalletPinDto { Pin = "0000" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.TooManyRequests, result.ErrorType);
        Assert.NotNull(user.WalletPinLockedUntil);
        Assert.True(user.WalletPinLockedUntil > DateTime.UtcNow);
    }

    [Fact]
    public async Task VerifyPinAsync_WhileLocked_RejectsEvenCorrectPinWithoutCountingAttempt()
    {
        var user = NewUser(BCrypt.Net.BCrypt.HashPassword("1234"), failedAttempts: 5, lockedUntil: DateTime.UtcNow.AddMinutes(10));
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.VerifyPinAsync(new VerifyWalletPinDto { Pin = "1234" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.TooManyRequests, result.ErrorType);
        // Блокировка ещё активна — счётчик не должен был измениться от этой попытки.
        Assert.Equal(5, user.WalletPinFailedAttempts);
    }

    [Fact]
    public async Task VerifyPinAsync_LockExpired_ResetsAndAcceptsCorrectPin()
    {
        var user = NewUser(BCrypt.Net.BCrypt.HashPassword("1234"), failedAttempts: 5, lockedUntil: DateTime.UtcNow.AddMinutes(-1));
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.VerifyPinAsync(new VerifyWalletPinDto { Pin = "1234" });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, user.WalletPinFailedAttempts);
        Assert.Null(user.WalletPinLockedUntil);
    }

    [Fact]
    public async Task ChangePinAsync_CorrectCurrentPin_UpdatesHash()
    {
        var user = NewUser(BCrypt.Net.BCrypt.HashPassword("1234"));
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.ChangePinAsync(new ChangeWalletPinDto { CurrentPin = "1234", NewPin = "5678" });

        Assert.True(result.IsSuccess);
        Assert.True(BCrypt.Net.BCrypt.Verify("5678", user.WalletPinHash!));
    }

    [Fact]
    public async Task ChangePinAsync_WrongCurrentPin_FailsAndDoesNotChangeHash()
    {
        var originalHash = BCrypt.Net.BCrypt.HashPassword("1234");
        var user = NewUser(originalHash);
        _userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.ChangePinAsync(new ChangeWalletPinDto { CurrentPin = "0000", NewPin = "5678" });

        Assert.False(result.IsSuccess);
        Assert.Equal(originalHash, user.WalletPinHash);
    }
}
