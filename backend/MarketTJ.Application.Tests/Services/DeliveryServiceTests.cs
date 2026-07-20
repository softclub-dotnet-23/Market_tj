using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class DeliveryServiceTests
{
    private readonly Mock<IDeliveryRepository> _deliveryRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICourierProfileRepository> _courierProfileRepository = new();
    private readonly Mock<ILogger<DeliveryService>> _logger = new();
    private readonly DeliveryService _service;

    public DeliveryServiceTests()
    {
        _service = new DeliveryService(_deliveryRepository.Object, _orderRepository.Object, _courierProfileRepository.Object, _logger.Object);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
        _courierProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CourierProfile
        {
            Id = id, UserId = 1, TransportType = "Car", VehicleNumber = "1234", Region = "Хатлон", District = "Бохтар", IsAvailable = true, IsActive = true
        });
        _deliveryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static Delivery CreateDelivery(int id = 1, int orderId = 1, int? courierId = null, DeliveryStatus status = DeliveryStatus.Pending) => new()
    {
        Id = id,
        OrderId = orderId,
        CourierId = courierId,
        PickupAddress = "Pickup",
        DeliveryAddress = "Delivery",
        DeliveryPrice = 10,
        Status = status,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateDeliveryDto ValidCreateDto(int orderId = 1, int? courierId = null) => new()
    {
        OrderId = orderId,
        CourierId = courierId,
        PickupAddress = "Pickup",
        DeliveryAddress = "Delivery",
        DeliveryPrice = 10,
        Status = DeliveryStatus.Pending
    };

    private static UpdateDeliveryDto ValidUpdateDto(int id = 1, int orderId = 1, int? courierId = null) => new()
    {
        Id = id,
        OrderId = orderId,
        CourierId = courierId,
        PickupAddress = "Pickup",
        DeliveryAddress = "Delivery",
        DeliveryPrice = 10,
        Status = DeliveryStatus.Pending
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_DeliveriesExist_ReturnsMappedDtos()
    {
        _deliveryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateDelivery(1), CreateDelivery(2, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _deliveryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var delivery = CreateDelivery(5);
        _deliveryRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(delivery);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(delivery.Id, result.Data!.Id);
        Assert.Equal(delivery.DeliveryAddress, result.Data!.DeliveryAddress);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _deliveryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsDeliveryAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroOrderId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.OrderId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyPickupAddress_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.PickupAddress = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyDeliveryAddress_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.DeliveryAddress = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeDeliveryPrice_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.DeliveryPrice = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = (DeliveryStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderAlreadyHasDelivery_ReturnsConflict()
    {
        _deliveryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateDelivery(1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CourierNotFound_ReturnsNotFound()
    {
        _courierProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CourierProfile?)null);

        var result = await _service.CreateAsync(ValidCreateDto(courierId: 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CourierHasActiveDelivery_ReturnsConflict()
    {
        _deliveryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateDelivery(1, 1, 5, DeliveryStatus.InDelivery)]);

        var result = await _service.CreateAsync(ValidCreateDto(2, 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CourierAssigned_SetsAssignedAt()
    {
        Delivery? added = null;
        _deliveryRepository.Setup(r => r.AddAsync(It.IsAny<Delivery>())).Callback<Delivery>(d => added = d).Returns(Task.CompletedTask);

        await _service.CreateAsync(ValidCreateDto(courierId: 5));

        Assert.NotNull(added!.AssignedAt);
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
    public async Task UpdateAsync_ValidData_UpdatesDeliveryAndReturnsOk()
    {
        var delivery = CreateDelivery(1);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeliveryNotFound_ReturnsNotFound()
    {
        _deliveryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyPickupAddress_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.PickupAddress = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OrderNotFound_ReturnsNotFound()
    {
        var delivery = CreateDelivery(1);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OrderAlreadyHasAnotherDelivery_ReturnsConflict()
    {
        var delivery = CreateDelivery(1, 1);
        var other = CreateDelivery(2, 2);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);
        _deliveryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([delivery, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CourierNotFound_ReturnsNotFound()
    {
        var delivery = CreateDelivery(1);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);
        _courierProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CourierProfile?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, courierId: 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CourierHasAnotherActiveDelivery_ReturnsConflict()
    {
        var delivery = CreateDelivery(1, 1);
        var other = CreateDelivery(2, 2, 5, DeliveryStatus.InDelivery);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);
        _deliveryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([delivery, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, courierId: 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingDelivery_DeletesAndReturnsOk()
    {
        var delivery = CreateDelivery(1);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.DeleteAsync(delivery), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DeliveryNotFound_ReturnsNotFound()
    {
        _deliveryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _deliveryRepository.Verify(r => r.DeleteAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _deliveryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
