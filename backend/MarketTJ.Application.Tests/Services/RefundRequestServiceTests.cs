using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.RefundRequestDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class RefundRequestServiceTests
{
    private readonly Mock<IRefundRequestRepository> _refundRequestRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICustomerProfileRepository> _customerProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ILogger<RefundRequestService>> _logger = new();
    private readonly RefundRequestService _service;

    public RefundRequestServiceTests()
    {
        _service = new RefundRequestService(_refundRequestRepository.Object, _orderRepository.Object, _customerProfileRepository.Object, _userRepository.Object, _auditLogService.Object, _logger.Object);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CustomerProfile { Id = id, UserId = 10, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Admin, FullName = "Admin", Email = "a@e.com", PhoneNumber = "1", PasswordHash = "h" });
        _refundRequestRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static RefundRequest CreateRequest(int id = 1, int orderId = 1, RefundStatus status = RefundStatus.Pending) => new()
    {
        Id = id,
        OrderId = orderId,
        CustomerId = 10,
        Reason = "Товар повреждён",
        Amount = 50,
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateRefundRequestDto ValidCreateDto(int orderId = 1) => new()
    {
        OrderId = orderId,
        CustomerId = 10,
        Reason = "Товар повреждён",
        Amount = 50,
        Status = RefundStatus.Pending
    };

    private static UpdateRefundRequestDto ValidUpdateDto(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        CustomerId = 10,
        Reason = "Товар повреждён",
        Amount = 50,
        Status = RefundStatus.Pending
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_RequestsExist_ReturnsMappedDtos()
    {
        _refundRequestRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateRequest(1), CreateRequest(2, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _refundRequestRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _refundRequestRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var request = CreateRequest(5);
        _refundRequestRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(request);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(request.Id, result.Data!.Id);
        Assert.Equal(request.Amount, result.Data!.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((RefundRequest?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsRequestAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroOrderId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.OrderId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyReason_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Reason = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroAmount_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Amount = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = (RefundStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CustomerIdMismatch_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CustomerId = 999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AmountExceedsOrderTotal_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Amount = 500;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ProcessedByAdminNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);
        var dto = ValidCreateDto();
        dto.ProcessedByAdminId = 5;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ProcessedByAdminNotAdminRole_ReturnsValidationError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { Id = 5, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
        var dto = ValidCreateDto();
        dto.ProcessedByAdminId = 5;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PendingAlreadyExistsForOrder_ReturnsConflict()
    {
        _refundRequestRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateRequest(1, 1, RefundStatus.Pending)]);

        var result = await _service.CreateAsync(ValidCreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _refundRequestRepository.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
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
    public async Task UpdateAsync_ValidData_UpdatesRequestAndReturnsOk()
    {
        var request = CreateRequest(1);
        _refundRequestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _refundRequestRepository.Verify(r => r.UpdateAsync(It.IsAny<RefundRequest>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RequestNotFound_ReturnsNotFound()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((RefundRequest?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _refundRequestRepository.Verify(r => r.UpdateAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AmountExceedsOrderTotal_ReturnsValidationError()
    {
        var request = CreateRequest(1);
        _refundRequestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        var dto = ValidUpdateDto(1);
        dto.Amount = 500;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _refundRequestRepository.Verify(r => r.UpdateAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_PendingAlreadyExistsOnAnotherRequest_ReturnsConflict()
    {
        var request = CreateRequest(1, 1, RefundStatus.Pending);
        var other = CreateRequest(2, 2, RefundStatus.Pending);
        _refundRequestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _refundRequestRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([request, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _refundRequestRepository.Verify(r => r.UpdateAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingRequest_DeletesAndReturnsOk()
    {
        var request = CreateRequest(1);
        _refundRequestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _refundRequestRepository.Verify(r => r.DeleteAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RequestNotFound_ReturnsNotFound()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((RefundRequest?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _refundRequestRepository.Verify(r => r.DeleteAsync(It.IsAny<RefundRequest>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
