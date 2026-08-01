using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class EmailVerificationServiceTests
{
    private readonly Mock<IEmailVerificationCodeRepository> _repository = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<EmailVerificationService>> _logger = new();
    private readonly EmailVerificationService _service;

    public EmailVerificationServiceTests()
    {
        _service = new EmailVerificationService(_repository.Object, _emailSender.Object, _configuration.Object, _logger.Object);
        _repository.Setup(r => r.GetLatestByEmailAsync(It.IsAny<string>())).ReturnsAsync((EmailVerificationCode?)null);
    }

    // ---------- SendCodeAsync ----------

    [Fact]
    public async Task SendCodeAsync_NoPreviousCode_SendsEmailAndStoresHashedCode()
    {
        var result = await _service.SendCodeAsync("user@example.com");

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.AddAsync(It.Is<EmailVerificationCode>(c => c.Email == "user@example.com" && !c.IsUsed && c.Attempts == 0)), Times.Once);
        _emailSender.Verify(e => e.SendAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendCodeAsync_NormalizesEmailCase()
    {
        var result = await _service.SendCodeAsync("User@Example.COM");

        Assert.True(result.IsSuccess);
        _emailSender.Verify(e => e.SendAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendCodeAsync_RecentCodeAlreadySent_ReturnsConflict()
    {
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(new EmailVerificationCode
        {
            Id = 1, Email = "user@example.com", CodeHash = "hash", CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });

        var result = await _service.SendCodeAsync("user@example.com");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _emailSender.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendCodeAsync_OldCodeExists_AllowsResend()
    {
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(new EmailVerificationCode
        {
            Id = 1, Email = "user@example.com", CodeHash = "hash", CreatedAt = DateTime.UtcNow.AddMinutes(-2), ExpiresAt = DateTime.UtcNow.AddMinutes(8)
        });

        var result = await _service.SendCodeAsync("user@example.com");

        Assert.True(result.IsSuccess);
        _emailSender.Verify(e => e.SendAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendCodeAsync_EmailSenderThrows_ReturnsInternalServerError()
    {
        _emailSender.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("smtp down"));

        var result = await _service.SendCodeAsync("user@example.com");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
        // Письмо не ушло — запись о коде не должна создаваться, иначе
        // следующая попытка натыкается на "подождите N сек" без реально
        // отправленного письма (см. Program.cs / SendCodeAsync).
        _repository.Verify(r => r.AddAsync(It.IsAny<EmailVerificationCode>()), Times.Never);
    }

    [Fact]
    public async Task SendCodeAsync_VerificationNotRequired_DoesNotCallEmailSenderButStillStoresCode()
    {
        _configuration.Setup(c => c["EmailVerification:RequireVerification"]).Returns("false");

        var result = await _service.SendCodeAsync("user@example.com");

        Assert.True(result.IsSuccess);
        _emailSender.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.AddAsync(It.IsAny<EmailVerificationCode>()), Times.Once);
    }

    // ---------- VerifyCodeAsync ----------

    private static EmailVerificationCode ValidCode(string email = "user@example.com", string code = "123456", int attempts = 0, bool isUsed = false, DateTime? expiresAt = null) => new()
    {
        Id = 1,
        Email = email,
        CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
        Attempts = attempts,
        IsUsed = isUsed,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task VerifyCodeAsync_VerificationNotRequired_AcceptsAnyCodeWithoutTouchingRepository()
    {
        _configuration.Setup(c => c["EmailVerification:RequireVerification"]).Returns("false");

        var result = await _service.VerifyCodeAsync("user@example.com", "000000");

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.GetLatestByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerifyCodeAsync_CorrectCode_MarksUsedAndReturnsOk()
    {
        var entity = ValidCode();
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(entity);

        var result = await _service.VerifyCodeAsync("user@example.com", "123456");

        Assert.True(result.IsSuccess);
        Assert.True(entity.IsUsed);
        _repository.Verify(r => r.UpdateAsync(entity), Times.Once);
    }

    [Fact]
    public async Task VerifyCodeAsync_WrongCode_IncrementsAttemptsAndReturnsValidationError()
    {
        var entity = ValidCode();
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(entity);

        var result = await _service.VerifyCodeAsync("user@example.com", "000000");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(1, entity.Attempts);
        Assert.False(entity.IsUsed);
    }

    [Fact]
    public async Task VerifyCodeAsync_NoCodeRequested_ReturnsValidationError()
    {
        var result = await _service.VerifyCodeAsync("nobody@example.com", "123456");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task VerifyCodeAsync_AlreadyUsed_ReturnsValidationError()
    {
        var entity = ValidCode(isUsed: true);
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(entity);

        var result = await _service.VerifyCodeAsync("user@example.com", "123456");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task VerifyCodeAsync_Expired_ReturnsValidationError()
    {
        var entity = ValidCode(expiresAt: DateTime.UtcNow.AddMinutes(-1));
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(entity);

        var result = await _service.VerifyCodeAsync("user@example.com", "123456");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task VerifyCodeAsync_TooManyAttempts_ReturnsValidationError()
    {
        var entity = ValidCode(attempts: 5);
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(entity);

        var result = await _service.VerifyCodeAsync("user@example.com", "123456");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<EmailVerificationCode>()), Times.Never);
    }

    [Fact]
    public async Task VerifyCodeAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _repository.Setup(r => r.GetLatestByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.VerifyCodeAsync("user@example.com", "123456");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- IsEmailVerifiedAsync ----------

    [Fact]
    public async Task IsEmailVerifiedAsync_UsedRecentCode_ReturnsTrue()
    {
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(ValidCode(isUsed: true));

        Assert.True(await _service.IsEmailVerifiedAsync("user@example.com"));
    }

    [Fact]
    public async Task IsEmailVerifiedAsync_NotUsed_ReturnsFalse()
    {
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(ValidCode(isUsed: false));

        Assert.False(await _service.IsEmailVerifiedAsync("user@example.com"));
    }

    [Fact]
    public async Task IsEmailVerifiedAsync_NoCodeAtAll_ReturnsFalse()
    {
        Assert.False(await _service.IsEmailVerifiedAsync("nobody@example.com"));
    }

    [Fact]
    public async Task IsEmailVerifiedAsync_UsedButTooOld_ReturnsFalse()
    {
        var entity = ValidCode(isUsed: true);
        entity.CreatedAt = DateTime.UtcNow.AddMinutes(-31);
        _repository.Setup(r => r.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(entity);

        Assert.False(await _service.IsEmailVerifiedAsync("user@example.com"));
    }
}
