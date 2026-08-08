using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class RateLimitServiceTests
{
    private readonly Mock<IAccountBlockService> _blockService = new();
    private readonly Mock<ILogger<RateLimitService>> _logger = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly RateLimitService _service;

    public RateLimitServiceTests()
    {
        _service = new RateLimitService(_cache, _blockService.Object, _logger.Object);
        _blockService.Setup(s => s.GetActiveBlockAsync(It.IsAny<int>())).ReturnsAsync((AccountBlock?)null);
    }

    private static AccountBlock MakeBlock(int minutes) => new()
    {
        Id = 1,
        UserId = 1,
        Role = "Customer",
        BlockType = "RateLimit",
        Reason = "test",
        BlockedAt = DateTime.UtcNow,
        BlockedUntil = DateTime.UtcNow.AddMinutes(minutes)
    };

    [Fact]
    public async Task CheckAsync_UnderLimit_ReturnsSuccessAndDoesNotCreateBlock()
    {
        for (var i = 0; i < 10; i++)
        {
            var result = await _service.CheckAsync(1, "Customer", "Chat.Send", maxRequests: 15, window: TimeSpan.FromMinutes(1));
            Assert.True(result.IsSuccess);
        }

        _blockService.Verify(s => s.CreateBlockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task CheckAsync_ExceedsLimit_CreatesBlockAndReturnsTooManyRequests()
    {
        _blockService.Setup(s => s.CreateBlockAsync(1, "Customer", "RateLimit", It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(MakeBlock(5));

        Result<string?> last = Result<string?>.Ok(null);
        for (var i = 0; i < 16; i++)
            last = await _service.CheckAsync(1, "Customer", "Chat.Send", maxRequests: 15, window: TimeSpan.FromMinutes(1));

        Assert.False(last.IsSuccess);
        Assert.Equal(ErrorType.TooManyRequests, last.ErrorType);
        Assert.Contains("заблокирован", last.Error);
        _blockService.Verify(s => s.CreateBlockAsync(1, "Customer", "RateLimit", It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task CheckAsync_ExactlyAtLimit_StillSucceeds()
    {
        Result<string?> last = Result<string?>.Ok(null);
        for (var i = 0; i < 15; i++)
            last = await _service.CheckAsync(1, "Customer", "Chat.Send", maxRequests: 15, window: TimeSpan.FromMinutes(1));

        Assert.True(last.IsSuccess);
        _blockService.Verify(s => s.CreateBlockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task CheckAsync_DifferentEndpointsCountedSeparately()
    {
        // 15 запросов к одному эндпоинту не должны влиять на лимит другого —
        // общий механизм должен быть переиспользуем на разных "чувствительных"
        // кнопках независимо друг от друга.
        for (var i = 0; i < 15; i++)
            await _service.CheckAsync(1, "Customer", "Chat.Send", maxRequests: 15, window: TimeSpan.FromMinutes(1));

        var result = await _service.CheckAsync(1, "Customer", "Cart.Add", maxRequests: 15, window: TimeSpan.FromMinutes(1));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CheckAsync_DifferentUsersCountedSeparately()
    {
        for (var i = 0; i < 15; i++)
            await _service.CheckAsync(1, "Customer", "Chat.Send", maxRequests: 15, window: TimeSpan.FromMinutes(1));

        var result = await _service.CheckAsync(2, "Customer", "Chat.Send", maxRequests: 15, window: TimeSpan.FromMinutes(1));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CheckAsync_AlreadyActivelyBlocked_ReturnsTooManyRequestsWithoutIncrementingCounter()
    {
        _blockService.Setup(s => s.GetActiveBlockAsync(1)).ReturnsAsync(MakeBlock(3));

        var result = await _service.CheckAsync(1, "Customer", "Chat.Send", maxRequests: 15, window: TimeSpan.FromMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.TooManyRequests, result.ErrorType);
        _blockService.Verify(s => s.CreateBlockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task CheckAsync_ExceedsLimit_UsesRateLimitFirstAndEscalatedDurationsNotCancellationDefaults()
    {
        TimeSpan? capturedFirst = null, capturedEscalated = null;
        _blockService.Setup(s => s.CreateBlockAsync(1, "Customer", "RateLimit", It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()))
            .Callback<int, string, string, string, TimeSpan?, TimeSpan?>((_, _, _, _, first, escalated) =>
            {
                capturedFirst = first;
                capturedEscalated = escalated;
            })
            .ReturnsAsync(MakeBlock(5));

        for (var i = 0; i < 16; i++)
            await _service.CheckAsync(1, "Customer", "Chat.Send", maxRequests: 15, window: TimeSpan.FromMinutes(1));

        Assert.Equal(TimeSpan.FromMinutes(5), capturedFirst);
        Assert.Equal(TimeSpan.FromMinutes(30), capturedEscalated);
    }
}
