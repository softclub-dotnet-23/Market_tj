using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.PaymentDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ILogger<PaymentService>> _logger = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _service = new PaymentService(_paymentRepository.Object, _orderRepository.Object, _logger.Object);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Delivered,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
    }

    private static Payment CreatePayment(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        Amount = 110,
        Method = PaymentMethod.Cash,
        Status = PaymentStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    private static CreatePaymentDto ValidCreateDto(int orderId = 1) => new()
    {
        OrderId = orderId,
        Amount = 110,
        Method = PaymentMethod.Cash,
        Status = PaymentStatus.Pending
    };

    private static UpdatePaymentDto ValidUpdateDto(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        Amount = 110,
        Method = PaymentMethod.Cash,
        Status = PaymentStatus.Pending
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_PaymentsExist_ReturnsMappedDtos()
    {
        _paymentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreatePayment(1), CreatePayment(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _paymentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _paymentRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var payment = CreatePayment(5);
        _paymentRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(payment);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(payment.Id, result.Data!.Id);
        Assert.Equal(payment.Amount, result.Data!.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _paymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Payment?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _paymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsPaymentAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroOrderId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.OrderId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroAmount_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Amount = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidMethod_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Method = (PaymentMethod)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = (PaymentStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
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
    public async Task UpdateAsync_ValidData_UpdatesPaymentAndReturnsOk()
    {
        var payment = CreatePayment(1);
        _paymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_PaymentNotFound_ReturnsNotFound()
    {
        _paymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Payment?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidMethod_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Method = (PaymentMethod)999;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OrderNotFound_ReturnsNotFound()
    {
        var payment = CreatePayment(1);
        _paymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _paymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingPayment_DeletesAndReturnsOk()
    {
        var payment = CreatePayment(1);
        _paymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _paymentRepository.Verify(r => r.DeleteAsync(payment), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PaymentNotFound_ReturnsNotFound()
    {
        _paymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Payment?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _paymentRepository.Verify(r => r.DeleteAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _paymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
