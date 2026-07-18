using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.NotificationDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<NotificationService>> _logger = new();
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService(_notificationRepository.Object, _userRepository.Object, _logger.Object);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
    }

    private static Notification CreateNotification(int id = 1, int userId = 1) => new()
    {
        Id = id,
        UserId = userId,
        Title = "Заказ принят",
        Message = "Ваш заказ принят фермером",
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateNotificationDto ValidCreateDto(int userId = 1) => new()
    {
        UserId = userId,
        Title = "Заказ принят",
        Message = "Ваш заказ принят фермером",
        IsRead = false
    };

    private static UpdateNotificationDto ValidUpdateDto(int id = 1, int userId = 1) => new()
    {
        Id = id,
        UserId = userId,
        Title = "Заказ принят",
        Message = "Ваш заказ принят фермером",
        IsRead = false
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_NotificationsExist_ReturnsMappedDtos()
    {
        _notificationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateNotification(1), CreateNotification(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _notificationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _notificationRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var notification = CreateNotification(5);
        _notificationRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(notification);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(notification.Id, result.Data!.Id);
        Assert.Equal(notification.Title, result.Data!.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Notification?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsNotificationAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroUserId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.UserId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Title = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyMessage_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Message = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
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
    public async Task UpdateAsync_ValidData_UpdatesNotificationAndReturnsOk()
    {
        var notification = CreateNotification(1);
        _notificationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notification);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _notificationRepository.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotificationNotFound_ReturnsNotFound()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Notification?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _notificationRepository.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyTitle_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Title = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _notificationRepository.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UserNotFound_ReturnsNotFound()
    {
        var notification = CreateNotification(1);
        _notificationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notification);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _notificationRepository.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingNotification_DeletesAndReturnsOk()
    {
        var notification = CreateNotification(1);
        _notificationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notification);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _notificationRepository.Verify(r => r.DeleteAsync(notification), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotificationNotFound_ReturnsNotFound()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Notification?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _notificationRepository.Verify(r => r.DeleteAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
