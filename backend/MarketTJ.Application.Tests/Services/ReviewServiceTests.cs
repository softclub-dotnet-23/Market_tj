using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.NotificationDto;
using MarketTJ.Application.Dto.ReviewDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
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
    private readonly Mock<ICustomerProfileRepository> _customerProfileRepository = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IReviewAutoReplyService> _reviewAutoReplyService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ILogger<ReviewService>> _logger = new();
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _service = new ReviewService(_reviewRepository.Object, _orderRepository.Object, _customerProfileRepository.Object, _farmerProfileRepository.Object, _userRepository.Object, _currentUser.Object, _reviewAutoReplyService.Object, _notificationService.Object, _logger.Object);
        // Дефолтный Order/Review — CustomerId=1/FarmerId=1; залогинены как
        // покупатель, чей CustomerProfile.Id=1.
        _currentUser.Setup(c => c.UserId).Returns(10);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Customer));
        _customerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 10, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });
        // GetAllAsync резолвит имя покупателя батчем (CustomerProfile→User) —
        // по умолчанию пустые списки, чтобы не падать в тестах, которые этого
        // не проверяют явно (см. GetAllAsync_ReviewsExist_ResolvesCustomerFullName).
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        // По умолчанию у фермера нет профиля/автоответ выключен — CreateAsync
        // тесты не задевают TryAutoReplyAsync, если явно не настроят иначе.
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);
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
    public async Task GetAllAsync_ReviewsExist_ResolvesCustomerFullName()
    {
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateReview(1)]);
        _customerProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([new CustomerProfile { Id = 1, UserId = 42, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" }]);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([new User { Id = 42, FullName = "Азиз Каримов", Email = "aziz@test.tj", PhoneNumber = "900000000", PasswordHash = "x", Role = UserRole.Customer }]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Азиз Каримов", result.Data!.Single().CustomerFullName);
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
    public async Task GetAllAsync_FarmerIdProvided_ReturnsOnlyThatFarmersReviews()
    {
        var forFarmer1 = CreateReview(1);
        forFarmer1.FarmerId = 1;
        var forFarmer2 = CreateReview(2);
        forFarmer2.FarmerId = 2;
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([forFarmer1, forFarmer2]);

        var result = await _service.GetAllAsync(farmerId: 1);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Data!);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task GetAllAsync_FarmerIdOmitted_ReturnsAllReviews()
    {
        var forFarmer1 = CreateReview(1);
        forFarmer1.FarmerId = 1;
        var forFarmer2 = CreateReview(2);
        forFarmer2.FarmerId = 2;
        _reviewRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([forFarmer1, forFarmer2]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
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

    // ---------- CreateAsync — автоответ AI (FarmerProfile.AutoReplyToReviewsEnabled) ----------

    [Fact]
    public async Task CreateAsync_FarmerHasAutoReplyEnabled_GeneratesAndSavesReply()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 2, FarmName = "Farm", Region = "Хатлон", District = "Бохтар",
            Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified,
            AutoReplyToReviewsEnabled = true,
        });
        _reviewAutoReplyService.Setup(s => s.GenerateReplyAsync(5, It.IsAny<string?>())).ReturnsAsync("Спасибо за отзыв!");

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _reviewRepository.Verify(r => r.UpdateAsync(It.Is<Review>(rv => rv.FarmerReply == "Спасибо за отзыв!" && rv.FarmerRepliedAt != null)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AutoReplyGenerated_NotifiesCustomer()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 2, FarmName = "Farm", Region = "Хатлон", District = "Бохтар",
            Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified,
            AutoReplyToReviewsEnabled = true,
        });
        _reviewAutoReplyService.Setup(s => s.GenerateReplyAsync(5, It.IsAny<string?>())).ReturnsAsync("Спасибо за отзыв!");
        _customerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 99, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });

        await _service.CreateAsync(ValidCreateDto());

        _notificationService.Verify(n => n.CreateAsync(It.Is<CreateNotificationDto>(d => d.UserId == 99 && d.Message.Contains("Спасибо за отзыв!"))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FarmerHasAutoReplyDisabled_DoesNotGenerateReply()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 2, FarmName = "Farm", Region = "Хатлон", District = "Бохтар",
            Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified,
            AutoReplyToReviewsEnabled = false,
        });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _reviewAutoReplyService.Verify(s => s.GenerateReplyAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AutoReplyEnabledButGenerationFails_StillCreatesReview()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 2, FarmName = "Farm", Region = "Хатлон", District = "Бохтар",
            Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified,
            AutoReplyToReviewsEnabled = true,
        });
        _reviewAutoReplyService.Setup(s => s.GenerateReplyAsync(It.IsAny<int>(), It.IsAny<string?>())).ReturnsAsync((string?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Once);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
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
    public async Task CreateAsync_ForeignOrder_ReturnsForbidden()
    {
        // audit 2026-07-28, находка 2.2 (IDOR): заказ реально принадлежит
        // другому CustomerProfile (999), не текущему пользователю (1) — новая
        // проверка срабатывает раньше старой internal-consistency-проверки.
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Order
        {
            Id = 1, OrderNumber = "ORD-1", CustomerId = 999, FarmerId = 1, Status = OrderStatus.Completed,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
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

    // ---------- ReplyAsync ----------
    // Отвечает фермер, которому адресован отзыв (FarmerId=1 у CreateReview) —
    // переключаем залогиненного пользователя с покупателя (дефолт в
    // конструкторе) на фермера с FarmerProfile.Id=1.

    private void LoginAsFarmerOwningReview()
    {
        // IsAdmin() — extension method (CurrentUserAuthorizationExtensions),
        // Moq не умеет мокать их напрямую — мокаем Role, IsAdmin() сама
        // прочитает его через реальную (немокнутую) реализацию.
        _currentUser.Setup(c => c.UserId).Returns(20);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(20)).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 20, FarmName = "Farm", Region = "Хатлон", District = "Бохтар",
            Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified
        });
    }

    [Fact]
    public async Task ReplyAsync_FarmerOwnsReview_UpdatesAndReturnsOk()
    {
        LoginAsFarmerOwningReview();
        var review = CreateReview(1);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);

        var result = await _service.ReplyAsync(1, new ReplyToReviewDto { Reply = "Спасибо за отзыв! Ждём вас снова." });

        Assert.True(result.IsSuccess);
        Assert.Equal("Спасибо за отзыв! Ждём вас снова.", review.FarmerReply);
        Assert.NotNull(review.FarmerRepliedAt);
        _reviewRepository.Verify(r => r.UpdateAsync(review), Times.Once);
    }

    [Fact]
    public async Task ReplyAsync_FarmerOwnsReview_NotifiesCustomer()
    {
        LoginAsFarmerOwningReview();
        var review = CreateReview(1);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);
        _customerProfileRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 99, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });

        await _service.ReplyAsync(1, new ReplyToReviewDto { Reply = "Спасибо за отзыв!" });

        _notificationService.Verify(n => n.CreateAsync(It.Is<CreateNotificationDto>(d => d.UserId == 99 && d.Message.Contains("Спасибо за отзыв!"))), Times.Once);
    }

    [Fact]
    public async Task ReplyAsync_ReviewNotFound_ReturnsNotFound()
    {
        LoginAsFarmerOwningReview();
        _reviewRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Review?)null);

        var result = await _service.ReplyAsync(999, new ReplyToReviewDto { Reply = "Спасибо!" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ReplyAsync_EmptyReply_ReturnsValidationError()
    {
        LoginAsFarmerOwningReview();

        var result = await _service.ReplyAsync(1, new ReplyToReviewDto { Reply = "" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _reviewRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ReplyAsync_FarmerDoesNotOwnReview_ReturnsForbidden()
    {
        // FarmerProfile.Id=2 — отзыв (FarmerId=1) адресован другому фермеру.
        _currentUser.Setup(c => c.UserId).Returns(21);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Farmer));
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(21)).ReturnsAsync(new FarmerProfile
        {
            Id = 2, UserId = 21, FarmName = "Other Farm", Region = "Хатлон", District = "Бохтар",
            Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified
        });
        var review = CreateReview(1);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);

        var result = await _service.ReplyAsync(1, new ReplyToReviewDto { Reply = "Спасибо!" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _reviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task ReplyAsync_Admin_CanReplyToAnyReview()
    {
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Admin));
        var review = CreateReview(1);
        _reviewRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);

        var result = await _service.ReplyAsync(1, new ReplyToReviewDto { Reply = "Спасибо от администрации!" });

        Assert.True(result.IsSuccess);
        _reviewRepository.Verify(r => r.UpdateAsync(review), Times.Once);
    }

    [Fact]
    public async Task ReplyAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        LoginAsFarmerOwningReview();
        _reviewRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.ReplyAsync(1, new ReplyToReviewDto { Reply = "Спасибо!" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
