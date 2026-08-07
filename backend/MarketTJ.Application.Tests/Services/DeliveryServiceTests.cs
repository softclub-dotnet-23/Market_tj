using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
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
    private readonly Mock<IOrderService> _orderService = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IGoogleGeocodingService> _geocodingService = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<IAccountBlockService> _accountBlockService = new();
    private readonly Mock<ILogger<DeliveryService>> _logger = new();
    private readonly DeliveryService _service;

    // Дефолтные координаты для заказа/курьеров в тестах ниже — реальные
    // значения роли не играют, важно только их взаимное расстояние
    // (см. GetAvailableCouriersAsync-тесты, где явно нужны "рядом"/"далеко").
    private const double DefaultLat = 38.5598;
    private const double DefaultLng = 68.7870;

    public DeliveryServiceTests()
    {
        _service = new DeliveryService(
            _deliveryRepository.Object, _orderRepository.Object, _orderItemRepository.Object, _courierProfileRepository.Object,
            _customerProfileRepository.Object, _farmerProfileRepository.Object, _userRepository.Object,
            _notificationService.Object, _auditLogService.Object, _orderService.Object, _currentUser.Object,
            _geocodingService.Object, _fileStorageService.Object, _accountBlockService.Object, _logger.Object);
        _orderService.Setup(s => s.CompleteAfterDeliveryAsync(It.IsAny<int>())).ReturnsAsync(Result<string>.Ok("Заказ завершён"));
        _fileStorageService.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("/uploads/delivery-proof/1/photo.jpg");
        _geocodingService.Setup(s => s.GeocodeAsync(It.IsAny<string>())).ReturnsAsync(Result<(double, double)>.Ok((DefaultLat, DefaultLng)));
        _accountBlockService.Setup(s => s.RecordCancellationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(Result<string?>.Ok(null));
        _accountBlockService.Setup(s => s.GetActiveBlockAsync(It.IsAny<int>())).ReturnsAsync((AccountBlock?)null);
        _accountBlockService.Setup(s => s.GetActiveBlockedUserIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new HashSet<int>());
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _orderItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CustomerProfile { Id = id, UserId = 20, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });
        // Admin по умолчанию — существующие тесты этого файла проверяют
        // бизнес-правила (конфликт курьера, дубли), а не конкретно IDOR-guard
        // (отдельные Forbidden-тесты добавлены ниже с явной сменой роли).
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Admin));
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            // FarmerStatus = HandedToCourier по умолчанию — большинство тестов
            // в этом файле работают с уже-назначенной доставкой (реалистичный
            // случай для DeliveryService), а AcceptAsync с 2026-08-04 проверяет
            // именно это поле перед тем, как разрешить курьеру принять доставку.
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            FarmerStatus = FarmerOrderStatus.HandedToCourier,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110,
            // Уже "геокодирован" по умолчанию — большинство тестов в этом файле
            // не про GetAvailableCouriersAsync и не должны неявно зависеть от
            // мока геокодирования.
            DeliveryLatitude = DefaultLat, DeliveryLongitude = DefaultLng,
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
    public async Task AssignCourierAsync_NewDelivery_CreatesAndSetsFarmerStatus()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);
        _deliveryRepository.Setup(r => r.GetByCourierIdAsync(It.IsAny<int>())).ReturnsAsync([]);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 20, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });

        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.AddAsync(It.Is<Delivery>(d => d.CourierId == 5 && d.Status == DeliveryStatus.Assigned && d.PickupAddress == "Farm Address")), Times.Once);
        // FarmerStatus, не Status — назначение курьера больше не пишет в общий
        // Order.Status (2026-08-04, разделение статусов фермера/курьера).
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.FarmerStatus == FarmerOrderStatus.HandedToCourier)), Times.Once);
        _notificationService.Verify(n => n.CreateAsync(It.IsAny<MarketTJ.Application.Dto.NotificationDto.CreateNotificationDto>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task AssignCourierAsync_OwningFarmer_Succeeds()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);
        _deliveryRepository.Setup(r => r.GetByCourierIdAsync(It.IsAny<int>())).ReturnsAsync([]);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 20, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });

        // Дефолтный мок GetByIdAsync(int) в конструкторе отдаёт Order с FarmerId = 1 — совпадает с профилем фермера выше.
        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignCourierAsync_NotOwningFarmerNorAdmin_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(99);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync(new FarmerProfile { Id = 2, UserId = 99, FarmName = "Other", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Other Address" });

        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task AssignCourierAsync_Admin_ReturnsForbidden()
    {
        // По прямому запросу пользователя (2026-08-05): Admin больше не может
        // назначать курьера ни основным, ни запасным путём — только фермер.
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);
        _deliveryRepository.Setup(r => r.GetByCourierIdAsync(It.IsAny<int>())).ReturnsAsync([]);

        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task AssignCourierAsync_CourierHasActiveDelivery_ReturnsConflict()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
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
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        // "Доставлено" теперь читается из CourierStatus, не из общего Status
        // (2026-08-04, разделение статусов) — Status у такого заказа остаётся
        // на CourierAssigned, реалистичный пост-миграционный кейс.
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.CourierAssigned,
            FarmerStatus = FarmerOrderStatus.HandedToCourier, CourierStatus = CourierOrderStatus.Delivered,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.AssignCourierAsync(1, ValidAssignDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AssignCourierAsync_ReplacingCourier_ResetsProgress()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
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

    // ---------- AssignManualCourierAsync ----------

    private static AssignManualCourierDto ValidManualAssignDto() => new()
    {
        CourierName = "Файзулло",
        CourierPhone = "+992900112233",
        DeliveryFee = 15,
    };

    [Fact]
    public async Task AssignManualCourierAsync_OwningFarmer_CreatesDeliveryWithoutCourierId()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Farm Address" });
        _deliveryRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>())).ReturnsAsync((Delivery?)null);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 20, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });

        var result = await _service.AssignManualCourierAsync(1, ValidManualAssignDto());

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.AddAsync(It.Is<Delivery>(d =>
            d.CourierId == null && d.ManualCourierName == "Файзулло" && d.ManualCourierPhone == "+992900112233" && d.Status == DeliveryStatus.Assigned)), Times.Once);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.FarmerStatus == FarmerOrderStatus.HandedToCourier)), Times.Once);
    }

    [Fact]
    public async Task AssignManualCourierAsync_NotOwningFarmer_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(99);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync(new FarmerProfile { Id = 2, UserId = 99, FarmName = "Other", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "Other Address" });

        var result = await _service.AssignManualCourierAsync(1, ValidManualAssignDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _deliveryRepository.Verify(r => r.AddAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task AssignManualCourierAsync_Admin_ReturnsForbidden()
    {
        var result = await _service.AssignManualCourierAsync(1, ValidManualAssignDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    // ---------- ConfirmManualDeliveryAsync ----------

    private static MemoryStream FakePhotoStream() => new([1, 2, 3, 4]);

    [Fact]
    public async Task ConfirmManualDeliveryAsync_ValidPhoto_MarksDeliveredAndCompletesOrder()
    {
        var delivery = CreateDelivery(id: 1, courierId: null, status: DeliveryStatus.Assigned);
        delivery.ManualCourierName = "Файзулло";
        delivery.ManualCourierPhone = "+992900112233";
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmManualDeliveryAsync(1, FakePhotoStream(), "photo.jpg", 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
        Assert.NotNull(delivery.DeliveredAt);
        Assert.NotNull(delivery.DeliveryProofPhotoUrl);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.CourierStatus == CourierOrderStatus.Delivered)), Times.Once);
        // По прямому запросу пользователя (2026-08-05): заказ завершается сам,
        // без ручного шага Admin — иначе клиент не может оставить отзыв.
        _orderService.Verify(s => s.CompleteAfterDeliveryAsync(1), Times.Once);
    }

    [Fact]
    public async Task ConfirmManualDeliveryAsync_InvalidFileExtension_ReturnsValidationError()
    {
        var delivery = CreateDelivery(id: 1, courierId: null, status: DeliveryStatus.Assigned);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmManualDeliveryAsync(1, FakePhotoStream(), "photo.exe", 4);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        _fileStorageService.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmManualDeliveryAsync_AlreadyDelivered_ReturnsConflict()
    {
        var delivery = CreateDelivery(id: 1, courierId: null, status: DeliveryStatus.Delivered);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmManualDeliveryAsync(1, FakePhotoStream(), "photo.jpg", 4);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ConfirmManualDeliveryAsync_HasCourierId_ReturnsConflict()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtClient);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmManualDeliveryAsync(1, FakePhotoStream(), "photo.jpg", 4);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
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
    public async Task AcceptAsync_SetsCourierStatusAccepted()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Assigned));

        var result = await _service.AcceptAsync(1);

        Assert.True(result.IsSuccess);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.CourierStatus == CourierOrderStatus.Accepted)), Times.Once);
    }

    [Fact]
    public async Task AcceptAsync_FarmerStatusNotHandedToCourier_ReturnsValidationError()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Assigned));
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending, FarmerStatus = null,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.AcceptAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
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

    [Fact]
    public async Task AcceptAsync_CourierCurrentlyBlocked_ReturnsForbidden()
    {
        // Блок 2 (2026-08-08) — заблокированный курьер не может принять даже
        // уже назначенную ему доставку, пока не истечёт срок блокировки.
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Assigned));
        _accountBlockService.Setup(s => s.GetActiveBlockAsync(30)).ReturnsAsync(new AccountBlock
        {
            Id = 1, UserId = 30, Role = "Courier", BlockType = "Cancellations", Reason = "3 отмены за 24 часа",
            BlockedAt = DateTime.UtcNow, BlockedUntil = DateTime.UtcNow.AddHours(48)
        });

        var result = await _service.AcceptAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    // ---------- CancelByCourierAsync ----------

    private const string ValidCancelReason = "Сломался автомобиль по дороге";

    [Fact]
    public async Task CancelByCourierAsync_ValidReason_CancelsDeliveryAndRecordsCancellation()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, orderId: 1, courierId: 5, status: DeliveryStatus.Accepted));

        var result = await _service.CancelByCourierAsync(1, ValidCancelReason);

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.Is<Delivery>(d =>
            d.Status == DeliveryStatus.Cancelled && d.CancelledAt != null && d.CancellationReason == ValidCancelReason)), Times.Once);
        _accountBlockService.Verify(s => s.RecordCancellationAsync(30, "Courier", 1, ValidCancelReason), Times.Once);
    }

    [Fact]
    public async Task CancelByCourierAsync_NotOwningCourier_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 999, status: DeliveryStatus.Accepted));

        var result = await _service.CancelByCourierAsync(1, ValidCancelReason);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _accountBlockService.Verify(s => s.RecordCancellationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelByCourierAsync_AlreadyDelivered_ReturnsConflict()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Delivered));

        var result = await _service.CancelByCourierAsync(1, ValidCancelReason);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CancelByCourierAsync_InvalidReason_ReturnsValidationAndDoesNotCancelDelivery()
    {
        // Причина не проходит валидацию AccountBlockService (одно слово) —
        // отмена доставки не должна произойти вообще.
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted));
        _accountBlockService.Setup(s => s.RecordCancellationAsync(30, "Courier", 1, "Занято"))
            .ReturnsAsync(Result<string?>.Fail("Опишите причину отмены подробнее", ErrorType.Validation));

        var result = await _service.CancelByCourierAsync(1, "Занято");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>()), Times.Never);
    }

    [Fact]
    public async Task CancelByCourierAsync_TriggersNewBan_AppendsNoticeToSuccessMessage()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted));
        _accountBlockService.Setup(s => s.RecordCancellationAsync(30, "Courier", 1, ValidCancelReason))
            .ReturnsAsync(Result<string?>.Ok("Аккаунт заблокирован до 10.08.2026 12:00 UTC. Причина: 3 отмены за 24 часа."));

        var result = await _service.CancelByCourierAsync(1, ValidCancelReason);

        Assert.True(result.IsSuccess);
        Assert.Contains("заблокирован", result.Data);
        // Отмена доставки, которая привела к бану, всё равно должна пройти —
        // бан запрещает брать НОВЫЕ доставки, а не завершать текущую.
        _deliveryRepository.Verify(r => r.UpdateAsync(It.Is<Delivery>(d => d.Status == DeliveryStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task CancelByCourierAsync_OrderWasCourierAssigned_ResetsOrderStatusToReadyForPickup()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, orderId: 1, courierId: 5, status: DeliveryStatus.Accepted));
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.CourierAssigned,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.CancelByCourierAsync(1, ValidCancelReason);

        Assert.True(result.IsSuccess);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.ReadyForPickup)), Times.Once);
    }

    // ---------- UpdateCourierStatusAsync ----------

    [Fact]
    public async Task UpdateCourierStatusAsync_ValidNextStep_Succeeds()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted));

        var result = await _service.UpdateCourierStatusAsync(1, new CourierStatusUpdateDto { Status = DeliveryStatus.InTransit });

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.Is<Delivery>(d => d.Status == DeliveryStatus.InTransit)), Times.Once);
    }

    [Fact]
    public async Task UpdateCourierStatusAsync_SkippingToConfirmStep_ReturnsValidationError()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted));

        // Единственный валидный переход с Accepted — на InTransit (упрощение
        // 2026-08-05); ArrivedAtClient с Accepted напрямую недостижим.
        var result = await _service.UpdateCourierStatusAsync(1, new CourierStatusUpdateDto { Status = DeliveryStatus.ArrivedAtClient });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCourierStatusAsync_ToInTransit_SetsPickedUpAt_UpdatesDeliveryOnlyNotOrder()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        // ArrivedAtFarmer — fallback-источник для доставок, застрявших на
        // промежуточном статусе до упрощения флоу (см. CourierTransitions).
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtFarmer));

        var result = await _service.UpdateCourierStatusAsync(1, new CourierStatusUpdateDto { Status = DeliveryStatus.InTransit });

        Assert.True(result.IsSuccess);
        _deliveryRepository.Verify(r => r.UpdateAsync(It.Is<Delivery>(d => d.Status == DeliveryStatus.InTransit && d.PickedUpAt != null)), Times.Once);
        // Промежуточные шаги курьера (PickedUp/InTransit/...) больше НЕ трогают
        // Order вообще — ни Status, ни CourierStatus (2026-08-04, разделение
        // статусов: только Delivery.Status меняется на этих шагах).
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    // ---------- ConfirmDeliveryAsync ----------

    [Fact]
    public async Task ConfirmDeliveryAsync_ValidPhoto_MarksDeliveredAndCompletesOrder()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtClient);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, FakePhotoStream(), "photo.jpg", 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
        Assert.NotNull(delivery.DeliveredAt);
        Assert.NotNull(delivery.DeliveryProofPhotoUrl);
        // CourierStatus, не Status (2026-08-04, разделение статусов).
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.CourierStatus == CourierOrderStatus.Delivered)), Times.Once);
        // По прямому запросу пользователя (2026-08-05): заказ завершается сам,
        // без ручного шага Admin — иначе клиент не может оставить отзыв.
        _orderService.Verify(s => s.CompleteAfterDeliveryAsync(1), Times.Once);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_FromInTransit_ValidPhoto_MarksDelivered()
    {
        // Упрощённый флоу (2026-08-05): подтверждение фото теперь доступно
        // сразу из InTransit, без обязательного отдельного статуса
        // ArrivedAtClient — это и есть новый основной путь курьера.
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.InTransit);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, FakePhotoStream(), "photo.jpg", 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_TooEarly_ReturnsConflict()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.Accepted);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, FakePhotoStream(), "photo.jpg", 4);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_InvalidFileExtension_DoesNotCompleteOrder()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtClient);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        await _service.ConfirmDeliveryAsync(1, FakePhotoStream(), "photo.exe", 4);

        _orderService.Verify(s => s.CompleteAfterDeliveryAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_FileTooLarge_ReturnsValidationError()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtClient);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 5, UserId = 30, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, FakePhotoStream(), "photo.jpg", 6 * 1024 * 1024);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(DeliveryStatus.ArrivedAtClient, delivery.Status);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_NotOwningCourier_ReturnsForbidden()
    {
        var delivery = CreateDelivery(id: 1, courierId: 5, status: DeliveryStatus.ArrivedAtClient);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _currentUser.Setup(c => c.UserId).Returns(30);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(30)).ReturnsAsync(new CourierProfile { Id = 99, UserId = 30, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _deliveryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(delivery);

        var result = await _service.ConfirmDeliveryAsync(1, FakePhotoStream(), "photo.jpg", 4);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
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
    public async Task GetAvailableCouriersAsync_FarmerWithoutProfile_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    private void SetUpFarmerForAvailableCouriers()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _currentUser.Setup(c => c.UserId).Returns(10);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(
            new FarmerProfile { Id = 1, UserId = 10, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A" });
        _deliveryRepository.Setup(r => r.GetActiveCountsByCourierIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new Dictionary<int, int>());
        _deliveryRepository.Setup(r => r.GetCompletedCountsByCourierIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new Dictionary<int, int>());
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, FullName = "C", Email = "c@test.tj", PhoneNumber = "900000000", PasswordHash = "x", Role = UserRole.Courier });
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_ReturnsCouriersSortedByDistanceAscending()
    {
        // Замена прежней "свой регион/район первым" (2026-08-05) — теперь
        // реальное расстояние (Haversine) от заказа до курьера. Курьер 2
        // географически ближе к заказу, хотя в другом районе/регионе.
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "РРП", District = "Турсунзаде", IsActive = true, IsAvailable = true, Latitude = 38.70, Longitude = 68.90 },
            new CourierProfile { Id = 2, UserId = 2, TransportType = "Автомобиль", VehicleNumber = "2", Region = "РРП", District = "Турсунзаде", IsActive = true, IsAvailable = true, Latitude = 38.57, Longitude = 68.80 },
        ]);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1 });

        Assert.True(result.IsSuccess);
        var couriers = result.Data!.ToList();
        Assert.Equal(2, couriers.Count);
        Assert.Equal(2, couriers[0].Id);
        Assert.Equal(1, couriers[1].Id);
        Assert.True(couriers[0].DistanceKm < couriers[1].DistanceKm);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_ExplicitRegionFilter_NarrowsToThatRegion()
    {
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = 38.57, Longitude = 68.80 },
            new CourierProfile { Id = 2, UserId = 2, TransportType = "Автомобиль", VehicleNumber = "2", Region = "РРП", District = "Турсунзаде", IsActive = true, IsAvailable = true, Latitude = 38.58, Longitude = 68.81 },
        ]);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1, Region = "РРП" });

        Assert.True(result.IsSuccess);
        var courier = Assert.Single(result.Data!);
        Assert.Equal(2, courier.Id);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_OnlyAvailableFilter_ExcludesUnavailable()
    {
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = 38.57, Longitude = 68.80 },
            new CourierProfile { Id = 2, UserId = 2, TransportType = "Автомобиль", VehicleNumber = "2", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = false, Latitude = 38.58, Longitude = 68.81 },
        ]);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1, OnlyAvailable = true });

        Assert.True(result.IsSuccess);
        var courier = Assert.Single(result.Data!);
        Assert.Equal(1, courier.Id);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_CourierHasActiveDelivery_ShownAsUnavailable()
    {
        // Баг с прод (2026-08-06): CourierProfile.IsAvailable — ручной тумблер
        // курьера, не отражает реальную занятость активной доставкой. Курьер
        // с IsAvailable=true, но с активной (не Delivered/Cancelled) Delivery,
        // должен показываться как занятый (isAvailable=false в DTO), а не
        // "Свободен" — иначе назначение падает на этапе сохранения.
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = 38.57, Longitude = 68.80 },
        ]);
        _deliveryRepository.Setup(r => r.GetActiveCountsByCourierIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 1 });

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1 });

        Assert.True(result.IsSuccess);
        var courier = Assert.Single(result.Data!);
        Assert.Equal(1, courier.Id);
        Assert.False(courier.IsAvailable);
        Assert.Equal(1, courier.ActiveDeliveries);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_OnlyAvailableFilter_ExcludesCourierWithActiveDelivery()
    {
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = 38.57, Longitude = 68.80 },
            new CourierProfile { Id = 2, UserId = 2, TransportType = "Автомобиль", VehicleNumber = "2", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = 38.58, Longitude = 68.81 },
        ]);
        _deliveryRepository.Setup(r => r.GetActiveCountsByCourierIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 1 });

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1, OnlyAvailable = true });

        Assert.True(result.IsSuccess);
        var courier = Assert.Single(result.Data!);
        Assert.Equal(2, courier.Id);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_SameDistance_OrdersByRatingDescending()
    {
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Rating = 3.0m, Latitude = 38.57, Longitude = 68.80 },
            new CourierProfile { Id = 2, UserId = 2, TransportType = "Автомобиль", VehicleNumber = "2", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Rating = 5.0m, Latitude = 38.57, Longitude = 68.80 },
        ]);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1 });

        Assert.True(result.IsSuccess);
        var couriers = result.Data!.ToList();
        Assert.Equal(2, couriers[0].Id);
        Assert.Equal(1, couriers[1].Id);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_CourierOutsideRadius_Excluded()
    {
        // Курьер 2 — заведомо больше 40 км от заказа (см. DefaultLat/DefaultLng
        // в конструкторе) — "не показывать вообще", не просто ниже в списке.
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = 38.57, Longitude = 68.80 },
            new CourierProfile { Id = 2, UserId = 2, TransportType = "Автомобиль", VehicleNumber = "2", Region = "Суғд", District = "Хуҷанд", IsActive = true, IsAvailable = true, Latitude = 39.50, Longitude = 69.60 },
        ]);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1 });

        Assert.True(result.IsSuccess);
        var courier = Assert.Single(result.Data!);
        Assert.Equal(1, courier.Id);
        Assert.True(courier.DistanceKm <= 40.0);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_BlockedCourier_ExcludedFromList()
    {
        // Блок 2 (2026-08-08) — заблокированный курьер не должен появляться
        // в списке кандидатов вообще, не просто "нельзя назначить".
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = 38.57, Longitude = 68.80 },
            new CourierProfile { Id = 2, UserId = 2, TransportType = "Автомобиль", VehicleNumber = "2", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = 38.57, Longitude = 68.80 },
        ]);
        _accountBlockService.Setup(s => s.GetActiveBlockedUserIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new HashSet<int> { 2 });

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1 });

        Assert.True(result.IsSuccess);
        var courier = Assert.Single(result.Data!);
        Assert.Equal(1, courier.Id);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_CourierWithoutCoordinates_Excluded()
    {
        SetUpFarmerForAvailableCouriers();
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new CourierProfile { Id = 1, UserId = 1, TransportType = "Автомобиль", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар", IsActive = true, IsAvailable = true, Latitude = null, Longitude = null },
        ]);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 1 });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_OrderNotFound_ReturnsNotFound()
    {
        SetUpFarmerForAvailableCouriers();
        _orderRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 999 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_OrderNotOwnedByFarmer_ReturnsForbidden()
    {
        SetUpFarmerForAvailableCouriers();
        _orderRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Order
        {
            Id = 2, OrderNumber = "ORD-2", CustomerId = 1, FarmerId = 99, Status = OrderStatus.Pending,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110,
        });

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 2 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_OrderNotYetGeocoded_GeocodesAndCachesCoordinates()
    {
        SetUpFarmerForAvailableCouriers();
        var order = new Order
        {
            Id = 3, OrderNumber = "ORD-3", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            DeliveryAddress = "Some address", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110,
        };
        _orderRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(order);
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 3 });

        Assert.True(result.IsSuccess);
        _geocodingService.Verify(s => s.GeocodeAsync(It.IsAny<string>()), Times.Once);
        _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == 3 && o.DeliveryLatitude == DefaultLat && o.DeliveryLongitude == DefaultLng)), Times.Once);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_GeocodingFails_ReturnsValidationError()
    {
        SetUpFarmerForAvailableCouriers();
        var order = new Order
        {
            Id = 4, OrderNumber = "ORD-4", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            DeliveryAddress = "Bad address", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110,
        };
        _orderRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(order);
        _geocodingService.Setup(s => s.GeocodeAsync(It.IsAny<string>())).ReturnsAsync(Result<(double, double)>.Fail("Адрес не найден", ErrorType.Validation));

        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter { OrderId = 4 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetAvailableCouriersAsync_Admin_ReturnsForbidden()
    {
        var result = await _service.GetAvailableCouriersAsync(new AvailableCourierFilter());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
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
