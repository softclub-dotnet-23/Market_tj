using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
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
    private readonly Mock<IOrderItemRepository> _orderItemRepository = new();
    private readonly Mock<ICourierProfileRepository> _courierProfileRepository = new();
    private readonly Mock<ICustomerProfileRepository> _customerProfileRepository = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<DeliveryService>> _logger = new();
    private readonly DeliveryService _service;

    public DeliveryServiceTests()
    {
        _service = new DeliveryService(
            _deliveryRepository.Object, _orderRepository.Object, _orderItemRepository.Object, _courierProfileRepository.Object,
            _customerProfileRepository.Object, _farmerProfileRepository.Object, _userRepository.Object,
            _notificationService.Object, _auditLogService.Object, _currentUser.Object, _logger.Object);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _orderItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CustomerProfile { Id = id, UserId = 20, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });
        // Admin по умолчанию — существующие тесты этого файла проверяют
        // бизнес-правила (конфликт курьера, дубли), а не конкретно IDOR-guard
        // (отдельные Forbidden-тесты добавлены ниже с явной сменой роли).
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Admin));
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
        _courierProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CourierProfile
        {
            Id = id, UserId = 1, TransportType = "Car", VehicleNumber = "1234", Region = "Хатлон", District = "Бохтар", IsAvailable = true, IsActive = true
        });
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
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
    public async Task GetByIdAsync_NotOwnerNotCourierNotAdmin_ReturnsForbidden()
    {
        // audit 2026-07-28, находка 2.2 (IDOR): не Admin, не участник заказа
        // (Order.CustomerId/FarmerId=1), не назначенный курьер.
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Customer));
        _currentUser.Setup(c => c.UserId).Returns(999);
        var delivery = CreateDelivery(5);
        _deliveryRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(delivery);

        var result = await _service.GetByIdAsync(5);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
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
        _deliveryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateDelivery(1, 1, 5, DeliveryStatus.InTransit)]);

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
        var other = CreateDelivery(2, 2, 5, DeliveryStatus.InTransit);
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

    // ---------- AssignCourierAsync ----------

    private static AssignCourierDto ValidAssignDto(int courierId = 5) => new()
    {
        CourierId = courierId,
        DeliveryFee = 20,
        EstimatedPickupAt = DateTime.UtcNow.AddHours(1),
        EstimatedDeliveryAt = DateTime.UtcNow.AddHours(3),
        AdminNote = "Осторожно, хрупкое",
    };

    [Fact]
    public async Task AssignCourierAsync_NewDelivery_CreatesAndSetsOrderStatus()
    {
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);
        _deliveryRepository.Setup(r => r.GetByCourierIdAsync(It.IsAny<int>())).ReturnsAsync([]);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 20, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });

        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.AddAsync(It.Is<Delivery>(d => d.CourierId == 5 && d.Status == DeliveryStatus.Assigned && d.PickupAddress == "Farm Address")), Times.Once);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.CourierAssigned)), Times.Once);
        _notificationService.Verify(n => n.CreateAsync(It.IsAny<MarketTJ.Application.Dto.NotificationDto.CreateNotificationDto>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task AssignCourierAsync_NotAdmin_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));

        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task AssignCourierAsync_CourierHasActiveDelivery_ReturnsConflict()
    {
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);
        _deliveryRepository.Setup(r => r.GetByCourierIdAsync(It.IsAny<int>())).ReturnsAsync([
            CreateDelivery(id: 99, orderId: 2, courierId: 5, status: DeliveryStatus.Accepted),
        ]);

        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AssignCourierAsync_OrderAlreadyDelivered_ReturnsConflict()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Delivered,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AssignCourierAsync_ReplacingCourier_ResetsProgress()
    {
        var existing = CreateDelivery(id: 7, orderId: 1, courierId: 3, status: DeliveryStatus.InTransit);
        existing.AcceptedAt = DateTime.UtcNow;
        existing.PickedUpAt = DateTime.UtcNow;
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync(existing);
        _deliveryRepository.Setup(r => r.GetByCourierIdAsync(It.IsAny<int>())).ReturnsAsync([]);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 20, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });

        var result = await _service.AssignCourierAsync(1, ValidAssignDto(courierId: 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, existing.CourierId);
        Assert.Equal(DeliveryStatus.Assigned, existing.Status);
        Assert.Null(existing.AcceptedAt);
        Assert.Null(existing.PickedUpAt);
    }

    // ---------- MarkReadyForPickupAsync ----------

    [Fact]
    public async Task MarkReadyForPickupAsync_FarmerOwnsOrderAndDeliveryExists_Succeeds()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A" });
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync(CreateDelivery(courierId: 5, status: DeliveryStatus.Assigned));

        var result = await _service.MarkReadyForPickupAsync(1);

        Assert.True(result.IsSuccess);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.ReadyForPickup)), Times.Once);
    }

    [Fact]
    public async Task MarkReadyForPickupAsync_NotOwningFarmer_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(99);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync(new FarmerProfile { Id = 2, UserId = 99, FarmName = "F2", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A" });

        var result = await _service.MarkReadyForPickupAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task MarkReadyForPickupAsync_NoDeliveryYet_ReturnsConflict()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A" });
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);

        var result = await _service.MarkReadyForPickupAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    // ---------- AcceptAsync ----------

    [Fact]
    public async Task AcceptAsync_AssignedToThisCourier_Succeeds()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Assigned));

        var result = await _service.AcceptAsync(1);

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.Is<Delivery>(d => d.Status == DeliveryStatus.Accepted && d.AcceptedAt != null)), Times.Once);
    }

    [Fact]
    public async Task AcceptAsync_DeliveryAssignedToAnotherCourier_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 999, status: DeliveryStatus.Assigned));

        var result = await _service.AcceptAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task AcceptAsync_WrongInitialStatus_ReturnsConflict()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted));

        var result = await _service.AcceptAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    // ---------- UpdateCourierStatusAsync ----------

    [Fact]
    public async Task UpdateCourierStatusAsync_ValidNextStep_Succeeds()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted));

        var result = await _service.UpdateCourierStatusAsync(1, new CourierStatusUpdateDto { Status = DeliveryStatus.GoingToFarmer });

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.Is<Delivery>(d => d.Status == DeliveryStatus.GoingToFarmer)), Times.Once);
    }

    [Fact]
    public async Task UpdateCourierStatusAsync_SkippingAStep_ReturnsValidationError()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted));

        // Пропускаем GoingToFarmer/ArrivedAtFarmer — сразу PickedUp.
        var result = await _service.UpdateCourierStatusAsync(1, new CourierStatusUpdateDto { Status = DeliveryStatus.PickedUp });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCourierStatusAsync_PickedUp_SyncsOrderStatusAndTimestamp()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtFarmer));

        var result = await _service.UpdateCourierStatusAsync(1, new CourierStatusUpdateDto { Status = DeliveryStatus.PickedUp });

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.Is<Delivery>(d => d.Status == DeliveryStatus.PickedUp && d.PickedUpAt != null)), Times.Once);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.PickedUp)), Times.Once);
    }

    // ---------- ConfirmDeliveryAsync ----------

    [Fact]
    public async Task ConfirmDeliveryAsync_CorrectCode_MarksDelivered()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtClient);
        delivery.ConfirmationCodeHash = BCrypt.Net.BCrypt.HashPassword("1234");
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, new ConfirmDeliveryDto { Code = "1234" });

        Assert.True(result.IsSuccess);
        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
        Assert.NotNull(delivery.DeliveredAt);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.Delivered)), Times.Once);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_WrongCode_ReturnsValidationAndIncrementsAttempts()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtClient);
        delivery.ConfirmationCodeHash = BCrypt.Net.BCrypt.HashPassword("1234");
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, new ConfirmDeliveryDto { Code = "0000" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(1, delivery.ConfirmationAttempts);
        Assert.Equal(DeliveryStatus.ArrivedAtClient, delivery.Status);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_TooManyAttempts_ReturnsValidationError()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtClient);
        delivery.ConfirmationCodeHash = BCrypt.Net.BCrypt.HashPassword("1234");
        delivery.ConfirmationAttempts = 5;
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, new ConfirmDeliveryDto { Code = "1234" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_NotArrivedAtClientYet_ReturnsConflict()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.InTransit);
        delivery.ConfirmationCodeHash = BCrypt.Net.BCrypt.HashPassword("1234");
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, new ConfirmDeliveryDto { Code = "1234" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    // ---------- CancelAsync ----------

    [Fact]
    public async Task CancelAsync_NotAdmin_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));

        var result = await _service.CancelAsync(1, new CancelDeliveryDto { Reason = "test" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task CancelAsync_AlreadyDelivered_ReturnsConflict()
    {
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Delivered));

        var result = await _service.CancelAsync(1, new CancelDeliveryDto { Reason = "test" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CancelAsync_ValidDelivery_SetsCancelledAndReason()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.CancelAsync(1, new CancelDeliveryDto { Reason = "Курьер заболел" });

        Assert.True(result.IsSuccess);
        Assert.Equal(DeliveryStatus.Cancelled, delivery.Status);
        Assert.Equal("Курьер заболел", delivery.CancellationReason);
        Assert.NotNull(delivery.CancelledAt);
    }

    // ---------- GetAvailableCouriersAsync ----------

    [Fact]
    public async Task GetAvailableCouriersAsync_NotAdmin_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_OnlyAvailableFilter_ExcludesUnavailable()
    {
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true },
            new CourierProfile { Id = 2, UserId = 2, TransportType = "Car", VehicleNumber = "2", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = false },
        ]);
        _deliveryRepository.Setup(r => r.GetActiveCountsByCourierIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new Dictionary<int, int>());
        _deliveryRepository.Setup(r => r.GetCompletedCountsByCourierIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new Dictionary<int, int>());
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, FullName = "C", Email = "c@test.tj", PhoneNumber = "900000000", PasswordHash = "x", Role = UserRole.Courier });

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OnlyAvailable = true });

        Assert.True(result.IsSuccess);
        var courier = Assert.Single(result.Data!);
        Assert.Equal(1, courier.Id);
    }

    // ---------- ReportProblemAsync ----------

    [Fact]
    public async Task ReportProblemAsync_Owner_SavesDescriptionAndNotifiesAdmins()
    {
        var delivery = CreateDelivery(id: 1, orderId: 1, courierId: 5, status: DeliveryStatus.InTransit);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new User { Id = 100, FullName = "Admin", Email = "a@test.tj", PhoneNumber = "900000001", PasswordHash = "x", Role = UserRole.Admin },
        ]);

        var result = await _service.ReportProblemAsync(1, new ReportDeliveryProblemDto { Description = "Адрес неверный" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Адрес неверный", delivery.ProblemDescription);
        _notificationService.Verify(n => n.CreateAsync(It.Is<MarketTJ.Application.Dto.NotificationDto.CreateNotificationDto>(d => d.UserId == 100)), Times.Once);
    }

    [Fact]
    public async Task ReportProblemAsync_NotOwner_ReturnsForbidden()
    {
        var delivery = CreateDelivery(id: 1, orderId: 1, courierId: 5, status: DeliveryStatus.InTransit);
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Customer));
        _currentUser.Setup(c => c.UserId).Returns(999);
        _customerProfileRepository.Setup(r => r.GetByUserIdAsync(999)).ReturnsAsync((CustomerProfile?)null);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(999)).ReturnsAsync((FarmerProfile?)null);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(999)).ReturnsAsync((CourierProfile?)null);

        var result = await _service.ReportProblemAsync(1, new ReportDeliveryProblemDto { Description = "x" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
