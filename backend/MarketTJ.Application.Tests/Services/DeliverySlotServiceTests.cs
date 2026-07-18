using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliverySlotDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class DeliverySlotServiceTests
{
    private readonly Mock<IDeliverySlotRepository> _deliverySlotRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ILogger<DeliverySlotService>> _logger = new();
    private readonly DeliverySlotService _service;

    public DeliverySlotServiceTests()
    {
        _service = new DeliverySlotService(_deliverySlotRepository.Object, _orderRepository.Object, _logger.Object);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
        _deliverySlotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static DeliverySlot CreateSlot(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        Date = DateTime.UtcNow.Date,
        TimeFrom = "10:00",
        TimeTo = "13:00",
        CreatedAt = DateTime.UtcNow
    };

    private static CreateDeliverySlotDto ValidCreateDto(int orderId = 1) => new()
    {
        OrderId = orderId,
        Date = DateTime.UtcNow.Date,
        TimeFrom = "10:00",
        TimeTo = "13:00"
    };

    private static UpdateDeliverySlotDto ValidUpdateDto(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        Date = DateTime.UtcNow.Date,
        TimeFrom = "10:00",
        TimeTo = "13:00"
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_SlotsExist_ReturnsMappedDtos()
    {
        _deliverySlotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateSlot(1), CreateSlot(2, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _deliverySlotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliverySlotRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var slot = CreateSlot(5);
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(slot);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(slot.Id, result.Data!.Id);
        Assert.Equal(slot.TimeFrom, result.Data!.TimeFrom);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DeliverySlot?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsSlotAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _deliverySlotRepository.Verify(r => r.AddAsync(It.IsAny<DeliverySlot>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroOrderId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.OrderId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.AddAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DefaultDate_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Date = default;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.AddAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidTimeFromFormat_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.TimeFrom = "25:99";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.AddAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidTimeToFormat_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.TimeTo = "not-a-time";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.AddAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_TimeFromAfterTimeTo_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.TimeFrom = "15:00";
        dto.TimeTo = "10:00";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.AddAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.AddAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderAlreadyHasSlot_ReturnsConflict()
    {
        _deliverySlotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateSlot(1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.AddAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesSlotAndReturnsOk()
    {
        var slot = CreateSlot(1);
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(slot);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _deliverySlotRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliverySlot>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SlotNotFound_ReturnsNotFound()
    {
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DeliverySlot?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OrderNotFound_ReturnsNotFound()
    {
        var slot = CreateSlot(1);
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(slot);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OrderAlreadyHasAnotherSlot_ReturnsConflict()
    {
        var slot = CreateSlot(1, 1);
        var other = CreateSlot(2, 2);
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(slot);
        _deliverySlotRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([slot, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.UpdateAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingSlot_DeletesAndReturnsOk()
    {
        var slot = CreateSlot(1);
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(slot);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _deliverySlotRepository.Verify(r => r.DeleteAsync(slot), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SlotNotFound_ReturnsNotFound()
    {
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((DeliverySlot?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliverySlotRepository.Verify(r => r.DeleteAsync(It.IsAny<DeliverySlot>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliverySlotRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
