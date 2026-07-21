using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.OrderItemDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class OrderItemServiceTests
{
    private readonly Mock<IOrderItemRepository> _orderItemRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IProductListingRepository> _productListingRepository = new();
    private readonly Mock<ILogger<OrderItemService>> _logger = new();
    private readonly OrderItemService _service;

    public OrderItemServiceTests()
    {
        _service = new OrderItemService(_orderItemRepository.Object, _orderRepository.Object, _productListingRepository.Object, _logger.Object);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new ProductListing
        {
            Id = id, FarmerProfileId = 1, ProductId = 1, Title = "Listing", RetailPricePerKg = 10,
            AvailableQuantity = 100, MinimumOrderQuantity = 1, QualityGrade = "A", Region = "Хатлон",
            District = "Бохтар", Address = "A", Status = ListingStatus.Active
        });
    }

    private static OrderItem CreateItem(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        ProductListingId = 1,
        ProductName = "Картофель",
        UnitPrice = 10,
        Quantity = 5,
        TotalPrice = 50,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateOrderItemDto ValidCreateDto(int orderId = 1) => new()
    {
        OrderId = orderId,
        ProductListingId = 1,
        ProductName = "Картофель",
        UnitPrice = 10,
        Quantity = 5
    };

    private static UpdateOrderItemDto ValidUpdateDto(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        ProductListingId = 1,
        ProductName = "Картофель",
        UnitPrice = 10,
        Quantity = 5
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ItemsExist_ReturnsMappedDtos()
    {
        _orderItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateItem(1), CreateItem(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _orderItemRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderItemRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var item = CreateItem(5);
        _orderItemRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(item);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(item.Id, result.Data!.Id);
        Assert.Equal(item.ProductName, result.Data!.ProductName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _orderItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((OrderItem?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsItemAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ComputesTotalPriceFromUnitPriceAndQuantity()
    {
        OrderItem? added = null;
        _orderItemRepository.Setup(r => r.AddAsync(It.IsAny<OrderItem>())).Callback<OrderItem>(i => added = i).Returns(Task.CompletedTask);
        var dto = ValidCreateDto();
        dto.UnitPrice = 7;
        dto.Quantity = 3;

        await _service.CreateAsync(dto);

        Assert.Equal(21, added!.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_ZeroOrderId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.OrderId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroProductListingId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ProductListingId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyProductName_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ProductName = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroUnitPrice_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.UnitPrice = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroQuantity_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Quantity = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CompletedOrder_ReturnsValidationError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ListingNotFound_ReturnsNotFound()
    {
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderItemRepository.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Never);
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
    public async Task UpdateAsync_ValidData_UpdatesItemAndReturnsOk()
    {
        var item = CreateItem(1);
        _orderItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _orderItemRepository.Verify(r => r.UpdateAsync(It.IsAny<OrderItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ItemNotFound_ReturnsNotFound()
    {
        _orderItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((OrderItem?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderItemRepository.Verify(r => r.UpdateAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyProductName_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.ProductName = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderItemRepository.Verify(r => r.UpdateAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OrderNotFound_ReturnsNotFound()
    {
        var item = CreateItem(1);
        _orderItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderItemRepository.Verify(r => r.UpdateAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CompletedOrder_ReturnsValidationError()
    {
        var item = CreateItem(1);
        _orderItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderItemRepository.Verify(r => r.UpdateAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ListingNotFound_ReturnsNotFound()
    {
        var item = CreateItem(1);
        _orderItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _productListingRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ProductListing?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderItemRepository.Verify(r => r.UpdateAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingItem_DeletesAndReturnsOk()
    {
        var item = CreateItem(1);
        _orderItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _orderItemRepository.Verify(r => r.DeleteAsync(item), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ItemNotFound_ReturnsNotFound()
    {
        _orderItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((OrderItem?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _orderItemRepository.Verify(r => r.DeleteAsync(It.IsAny<OrderItem>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
