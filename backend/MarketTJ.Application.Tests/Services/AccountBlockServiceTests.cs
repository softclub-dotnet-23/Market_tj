using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class AccountBlockServiceTests
{
    private readonly Mock<IAccountBlockRepository> _blockRepository = new();
    private readonly Mock<IOrderCancellationRepository> _cancellationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<AccountBlockService>> _logger = new();
    private readonly AccountBlockService _service;

    public AccountBlockServiceTests()
    {
        _service = new AccountBlockService(_blockRepository.Object, _cancellationRepository.Object, _userRepository.Object, _currentUser.Object, _logger.Object);
        _blockRepository.Setup(r => r.GetActiveAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync((AccountBlock?)null);
        _blockRepository.Setup(r => r.CountPriorAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(0);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private const string ValidReason = "Заказ невозможно доставить вовремя";

    // ---------- RecordCancellationAsync — валидация причины ----------

    [Fact]
    public async Task RecordCancellationAsync_EmptyReason_FailsValidation()
    {
        var result = await _service.RecordCancellationAsync(1, "Courier", 100, "");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cancellationRepository.Verify(r => r.AddAsync(It.IsAny<OrderCancellation>()), Times.Never);
    }

    [Fact]
    public async Task RecordCancellationAsync_SingleWordReason_FailsValidation()
    {
        var result = await _service.RecordCancellationAsync(1, "Courier", 100, "Занят");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _cancellationRepository.Verify(r => r.AddAsync(It.IsAny<OrderCancellation>()), Times.Never);
    }

    [Fact]
    public async Task RecordCancellationAsync_TooShortReason_FailsValidation()
    {
        // 3+ слова, но короче 10 символов в сумме — тоже должно быть отклонено.
        var result = await _service.RecordCancellationAsync(1, "Courier", 100, "а б в");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task RecordCancellationAsync_ValidReason_SavesCancellation()
    {
        _cancellationRepository.Setup(r => r.CountSinceAsync(1, "Courier", It.IsAny<DateTime>())).ReturnsAsync(1);

        var result = await _service.RecordCancellationAsync(1, "Courier", 100, ValidReason);

        Assert.True(result.IsSuccess);
        _cancellationRepository.Verify(r => r.AddAsync(It.Is<OrderCancellation>(c =>
            c.UserId == 1 && c.Role == "Courier" && c.OrderId == 100 && c.Reason == ValidReason)), Times.Once);
    }

    // ---------- Порог 3 отмены за 24ч ----------

    [Fact]
    public async Task RecordCancellationAsync_BelowThreshold_DoesNotCreateBlock()
    {
        _cancellationRepository.Setup(r => r.CountSinceAsync(1, "Courier", It.IsAny<DateTime>())).ReturnsAsync(2);

        var result = await _service.RecordCancellationAsync(1, "Courier", 100, ValidReason);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
        _blockRepository.Verify(r => r.AddAsync(It.IsAny<AccountBlock>()), Times.Never);
    }

    [Fact]
    public async Task RecordCancellationAsync_ReachesThreeWithin24h_CreatesBlockAndReturnsNotice()
    {
        _cancellationRepository.Setup(r => r.CountSinceAsync(1, "Courier", It.IsAny<DateTime>())).ReturnsAsync(3);

        var result = await _service.RecordCancellationAsync(1, "Courier", 100, ValidReason);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Contains("заблокирован", result.Data);
        _blockRepository.Verify(r => r.AddAsync(It.Is<AccountBlock>(b =>
            b.UserId == 1 && b.Role == "Courier" && b.BlockType == AccountBlockService.CancellationsBlockType)), Times.Once);
    }

    [Fact]
    public async Task RecordCancellationAsync_AlreadyBlocked_DoesNotCreateDuplicateBlockOrNotice()
    {
        // 4-я, 5-я и т.д. отмена во время уже активного бана не должна снова
        // "создавать" бан и снова возвращать уведомление о новой блокировке.
        _cancellationRepository.Setup(r => r.CountSinceAsync(1, "Courier", It.IsAny<DateTime>())).ReturnsAsync(4);
        _blockRepository.Setup(r => r.GetActiveAsync(1, It.IsAny<DateTime>())).ReturnsAsync(new AccountBlock
        {
            Id = 1, UserId = 1, Role = "Courier", BlockType = AccountBlockService.CancellationsBlockType,
            Reason = "ранее", BlockedAt = DateTime.UtcNow.AddHours(-1), BlockedUntil = DateTime.UtcNow.AddHours(47)
        });

        var result = await _service.RecordCancellationAsync(1, "Courier", 100, ValidReason);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
        _blockRepository.Verify(r => r.AddAsync(It.IsAny<AccountBlock>()), Times.Never);
    }

    [Fact]
    public async Task RecordCancellationAsync_FarmerRole_CountsSeparatelyFromCourier()
    {
        // CountSinceAsync получает role — фермерские и курьерские нарушения не
        // должны смешиваться в одном счётчике для одного и того же userId.
        _cancellationRepository.Setup(r => r.CountSinceAsync(1, "Farmer", It.IsAny<DateTime>())).ReturnsAsync(3);

        await _service.RecordCancellationAsync(1, "Farmer", 100, ValidReason);

        _cancellationRepository.Verify(r => r.CountSinceAsync(1, "Farmer", It.IsAny<DateTime>()), Times.Once);
        _cancellationRepository.Verify(r => r.CountSinceAsync(1, "Courier", It.IsAny<DateTime>()), Times.Never);
    }

    // ---------- Длительность бана / эскалация ----------

    [Fact]
    public async Task CreateBlockAsync_FirstOffense_BlocksFor48Hours()
    {
        _blockRepository.Setup(r => r.CountPriorAsync(1, "Cancellations")).ReturnsAsync(0);

        var block = await _service.CreateBlockAsync(1, "Courier", "Cancellations", "test");

        var expected = DateTime.UtcNow.AddHours(48);
        Assert.True(Math.Abs((block.BlockedUntil - expected).TotalMinutes) < 1);
    }

    [Fact]
    public async Task CreateBlockAsync_RepeatOffense_EscalatesToSevenDays()
    {
        _blockRepository.Setup(r => r.CountPriorAsync(1, "Cancellations")).ReturnsAsync(1);

        var block = await _service.CreateBlockAsync(1, "Courier", "Cancellations", "test");

        var expected = DateTime.UtcNow.AddDays(7);
        Assert.True(Math.Abs((block.BlockedUntil - expected).TotalMinutes) < 1);
    }

    [Fact]
    public async Task CreateBlockAsync_OverrideDuration_IgnoresEscalationLogic()
    {
        // Блок 3 (rate-limit) переиспользует этот же метод с фиксированной
        // длительностью первого нарушения (5 минут), не 48ч.
        var block = await _service.CreateBlockAsync(1, "Customer", "RateLimit", "spam", TimeSpan.FromMinutes(5));

        var expected = DateTime.UtcNow.AddMinutes(5);
        Assert.True(Math.Abs((block.BlockedUntil - expected).TotalSeconds) < 30);
        _blockRepository.Verify(r => r.CountPriorAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- GetActiveBlockAsync ----------

    [Fact]
    public async Task GetActiveBlockAsync_DelegatesToRepositoryWithCurrentTime()
    {
        var block = new AccountBlock { Id = 1, UserId = 5, Role = "Courier", BlockType = "Cancellations", Reason = "r", BlockedAt = DateTime.UtcNow, BlockedUntil = DateTime.UtcNow.AddHours(1) };
        _blockRepository.Setup(r => r.GetActiveAsync(5, It.IsAny<DateTime>())).ReturnsAsync(block);

        var result = await _service.GetActiveBlockAsync(5);

        Assert.Same(block, result);
    }

    // ---------- UnblockAsync ----------

    [Fact]
    public async Task UnblockAsync_NotAdmin_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(1);

        var result = await _service.UnblockAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task UnblockAsync_BlockNotFound_ReturnsNotFound()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Admin));
        _currentUser.Setup(c => c.UserId).Returns(99);
        _blockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((AccountBlock?)null);

        var result = await _service.UnblockAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UnblockAsync_AlreadyUnblocked_ReturnsConflict()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Admin));
        _currentUser.Setup(c => c.UserId).Returns(99);
        _blockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new AccountBlock
        {
            Id = 1, UserId = 5, Role = "Courier", BlockType = "Cancellations", Reason = "r",
            BlockedAt = DateTime.UtcNow, BlockedUntil = DateTime.UtcNow.AddHours(1), UnblockedAt = DateTime.UtcNow
        });

        var result = await _service.UnblockAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UnblockAsync_ActiveBlock_ClearsItAndRecordsAdmin()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Admin));
        _currentUser.Setup(c => c.UserId).Returns(99);
        var block = new AccountBlock
        {
            Id = 1, UserId = 5, Role = "Courier", BlockType = "Cancellations", Reason = "r",
            BlockedAt = DateTime.UtcNow, BlockedUntil = DateTime.UtcNow.AddHours(1)
        };
        _blockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(block);

        var result = await _service.UnblockAsync(1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(block.UnblockedAt);
        Assert.Equal(99, block.UnblockedByAdminId);
        _blockRepository.Verify(r => r.UpdateAsync(block), Times.Once);
    }

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ActiveOnlyTrue_FiltersOutExpiredAndManuallyUnblocked()
    {
        var now = DateTime.UtcNow;
        _blockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new AccountBlock { Id = 1, UserId = 1, Role = "Courier", BlockType = "Cancellations", Reason = "r", BlockedAt = now, BlockedUntil = now.AddHours(1) },
            new AccountBlock { Id = 2, UserId = 2, Role = "Courier", BlockType = "Cancellations", Reason = "r", BlockedAt = now.AddDays(-3), BlockedUntil = now.AddDays(-2) },
            new AccountBlock { Id = 3, UserId = 3, Role = "Farmer", BlockType = "Cancellations", Reason = "r", BlockedAt = now, BlockedUntil = now.AddHours(1), UnblockedAt = now }
        ]);

        var result = await _service.GetAllAsync(new PagedRequest(), activeOnly: true);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
        Assert.Equal(1, result.Data.Items.First().Id);
        Assert.True(result.Data.Items.First().IsActive);
    }

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsAllOrderedByMostRecent()
    {
        var now = DateTime.UtcNow;
        _blockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new AccountBlock { Id = 1, UserId = 1, Role = "Courier", BlockType = "Cancellations", Reason = "r", BlockedAt = now.AddDays(-1), BlockedUntil = now.AddHours(1) },
            new AccountBlock { Id = 2, UserId = 2, Role = "Farmer", BlockType = "Cancellations", Reason = "r", BlockedAt = now, BlockedUntil = now.AddHours(1) }
        ]);

        var result = await _service.GetAllAsync(new PagedRequest(), activeOnly: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.TotalCount);
        Assert.Equal(2, result.Data.Items.First().Id);
    }

    // ---------- GetActiveBlockedUserIdsAsync ----------

    [Fact]
    public async Task GetActiveBlockedUserIdsAsync_DelegatesToRepository()
    {
        _blockRepository.Setup(r => r.GetActiveUserIdsAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1, 2, 3 })), It.IsAny<DateTime>())).ReturnsAsync([2]);

        var result = await _service.GetActiveBlockedUserIdsAsync([1, 2, 3]);

        Assert.Single(result);
        Assert.Contains(2, result);
    }
}
