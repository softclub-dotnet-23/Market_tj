using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.OrderDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICustomerProfileRepository> _customerProfileRepository = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ILogger<OrderService>> _logger = new();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(_orderRepository.Object, _customerProfileRepository.Object, _farmerProfileRepository.Object, _userRepository.Object, _auditLogService.Object, _logger.Object);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CustomerProfile { Id = id, UserId = 10, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new FarmerProfile { Id = id, UserId = 20, FarmName = "Farm", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified });
        _userRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new User { Id = 10, IsActive = true, Role = UserRole.Customer, FullName = "C", Email = "c@e.com", PhoneNumber = "1", PasswordHash = "h" });
        _orderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static Order CreateOrder(int id = 1, OrderStatus status = OrderStatus.Pending, string orderNumber = "ORD-1") => new()
    {
        Id = id,
        OrderNumber = orderNumber,
        CustomerId = 1,
        FarmerId = 1,
        Status = status,
        DeliveryAddress = "Address",
        Region = "Хатлон",
        District = "Бохтар",
        Subtotal = 100,
        DeliveryPrice = 10,
        TotalAmount = 110,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateOrderDto ValidCreateDto(string orderNumber = "ORD-1") => new()
    {
        OrderNumber = orderNumber,
        CustomerId = 1,
        FarmerId = 1,
        Status = OrderStatus.Pending,
        DeliveryAddress = "Address",
        Region = "Хатлон",
        District = "Бохтар",
        Subtotal = 100,
        DeliveryPrice = 10,
        TotalAmount = 110
    };

    private static UpdateOrderDto ValidUpdateDto(int id = 1, OrderStatus status = OrderStatus.Pending, string orderNumber = "ORD-1") => new()
    {
        Id = id,
        OrderNumber = orderNumber,
        CustomerId = 1,
        FarmerId = 1,
        Status = status,
        DeliveryAddress = "Address",
        Region = "Хатлон",
        District = "Бохтар",
        Subtotal = 100,
        DeliveryPrice = 10,
        TotalAmount = 110
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_OrdersExist_ReturnsMappedDtos()
    {
        _orderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateOrder(1), CreateOrder(2, orderNumber: "ORD-2")]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _orderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var order = CreateOrder(5);
        _orderRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(order);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(order.Id, result.Data!.Id);
        Assert.Equal(order.OrderNumber, result.Data!.OrderNumber);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsOrderAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AlwaysForcesPendingStatus()
    {
        Order? added = null;
        _orderRepository.Setup(r => r.AddAsync(It.IsAny<Order>())).Callback<Order>(o => added = o).Returns(Task.CompletedTask);
        var dto = ValidCreateDto();
        dto.Status = OrderStatus.Completed;

        await _service.CreateAsync(dto);

        Assert.Equal(OrderStatus.Pending, added!.Status);
    }

    [Fact]
    public async Task CreateAsync_RecomputesTotalAmountFromSubtotalAndDeliveryPrice()
    {
        Order? added = null;
        _orderRepository.Setup(r => r.AddAsync(It.IsAny<Order>())).Callback<Order>(o => added = o).Returns(Task.CompletedTask);
        var dto = ValidCreateDto();
        dto.Subtotal = 50;
        dto.DeliveryPrice = 5;
        dto.TotalAmount = 999;

        await _service.CreateAsync(dto);

        Assert.Equal(55, added!.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_EmptyOrderNumber_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.OrderNumber = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroCustomerId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CustomerId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroFarmerId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FarmerId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyDeliveryAddress_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.DeliveryAddress = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyRegion_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Region = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyDistrict_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.District = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeSubtotal_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Subtotal = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeDeliveryPrice_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.DeliveryPrice = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NegativeTotalAmount_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.TotalAmount = -1;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = (OrderStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CustomerProfileNotFound_ReturnsNotFound()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CustomerProfile?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InactiveCustomer_ReturnsValidationError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new User { Id = 10, IsActive = false, Role = UserRole.Customer, FullName = "C", Email = "c@e.com", PhoneNumber = "1", PasswordHash = "h" });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FarmerProfileNotFound_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnverifiedFarmer_ReturnsValidationError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 20, FarmName = "Farm", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A",
            VerificationStatus = FarmerVerificationStatus.Pending
        });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateOrderNumber_ReturnsConflict()
    {
        _orderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateOrder(1, orderNumber: "ORD-1")]);

        var result = await _service.CreateAsync(ValidCreateDto("ORD-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesOrderAndReturnsOk()
    {
        var order = CreateOrder(1);
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CompletedOrder_ReturnsValidationErrorAndDoesNotUpdate()
    {
        var order = CreateOrder(1, OrderStatus.Completed);
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyOrderNumber_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.OrderNumber = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CustomerProfileNotFound_ReturnsNotFound()
    {
        var order = CreateOrder(1);
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CustomerProfile?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_FarmerProfileNotFound_ReturnsNotFound()
    {
        var order = CreateOrder(1);
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateOrderNumberOnAnotherOrder_ReturnsConflict()
    {
        var order = CreateOrder(1, orderNumber: "ORD-1");
        var other = CreateOrder(2, orderNumber: "ORD-2");
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _orderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([order, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, orderNumber: "ORD-2"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_StatusChangedToAccepted_SetsAcceptedAt()
    {
        var order = CreateOrder(1);
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        await _service.UpdateAsync(1, ValidUpdateDto(1, OrderStatus.Accepted));

        Assert.NotNull(order.AcceptedAt);
    }

    [Fact]
    public async Task UpdateAsync_StatusChangedToCompleted_SetsCompletedAt()
    {
        var order = CreateOrder(1);
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        await _service.UpdateAsync(1, ValidUpdateDto(1, OrderStatus.Completed));

        Assert.NotNull(order.CompletedAt);
    }

    [Fact]
    public async Task UpdateAsync_StatusChangedToCancelled_SetsCancelledAt()
    {
        var order = CreateOrder(1);
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        await _service.UpdateAsync(1, ValidUpdateDto(1, OrderStatus.Cancelled));

        Assert.NotNull(order.CancelledAt);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingOrder_SoftDeletesAndReturnsOk()
    {
        var order = CreateOrder(1);
        _orderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(order.IsDeleted);
        Assert.NotNull(order.DeletedAt);
        _orderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
