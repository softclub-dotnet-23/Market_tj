using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ReviewDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class ReviewServiceTests
{
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ILogger<ReviewService>> _logger = new();
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _service = new ReviewService(_reviewRepository.Object, _orderRepository.Object, _logger.Object);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static Review CreateReview(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        CustomerId = 1,
        FarmerId = 1,
        Rating = 5,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateReviewDto ValidCreateDto(int orderId = 1) => new()
    {
        OrderId = orderId,
        CustomerId = 1,
        FarmerId = 1,
        Rating = 5
    };

    private static UpdateReviewDto ValidUpdateDto(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        CustomerId = 1,
        FarmerId = 1,
        Rating = 5
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ReviewsExist_ReturnsMappedDtos()
    {
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateReview(1), CreateReview(2, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _reviewRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var review = CreateReview(5);
        _reviewRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(review);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(review.Id, result.Data!.Id);
        Assert.Equal(review.Rating, result.Data!.Rating);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _reviewRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Review?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _reviewRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsReviewAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroOrderId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.OrderId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroCustomerId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CustomerId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroFarmerId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FarmerId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RatingBelowOne_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Rating = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RatingAboveFive_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Rating = 6;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNotCompleted_ReturnsValidationError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ForeignOrder_ReturnsUnauthorized()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 999, FarmerId = 1, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FarmerIdMismatch_ReturnsValidationError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 999, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReviewAlreadyExistsForOrder_ReturnsConflict()
    {
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateReview(1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
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
    public async Task UpdateAsync_ValidData_UpdatesReviewAndReturnsOk()
    {
        var review = CreateReview(1);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReviewNotFound_ReturnsNotFound()
    {
        _reviewRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Review?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RatingOutOfRange_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Rating = 10;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OrderNotFound_ReturnsNotFound()
    {
        var review = CreateReview(1);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ForeignOrder_ReturnsUnauthorized()
    {
        var review = CreateReview(1);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 999, FarmerId = 1, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ReviewAlreadyExistsOnAnotherReview_ReturnsConflict()
    {
        var review = CreateReview(1, 1);
        var other = CreateReview(2, 2);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([review, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _reviewRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingReview_DeletesAndReturnsOk()
    {
        var review = CreateReview(1);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _reviewRepository.Verify(r => r.DeleteAsync(review), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReviewNotFound_ReturnsNotFound()
    {
        _reviewRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Review?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _reviewRepository.Verify(r => r.DeleteAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _reviewRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
