using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ConversationDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class ConversationServiceTests
{
    private readonly Mock<IConversationRepository> _conversationRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICustomerProfileRepository> _customerProfileRepository = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<ConversationService>> _logger = new();
    private readonly ConversationService _service;

    public ConversationServiceTests()
    {
        _service = new ConversationService(_conversationRepository.Object, _orderRepository.Object, _customerProfileRepository.Object, _farmerProfileRepository.Object, _currentUser.Object, _logger.Object);
        // Conversation.CustomerId/FarmerId — User.Id напрямую; дефолтный чат
        // (CreateConversation) имеет CustomerId=10 — залогинены как этот покупатель.
        _currentUser.Setup(c => c.UserId).Returns(10);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Customer));
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Order
        {
            Id = id, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = OrderStatus.Pending,
            DeliveryAddress = "A", Region = "Хатлон", District = "Бохтар", Subtotal = 100, DeliveryPrice = 10, TotalAmount = 110
        });
        _customerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CustomerProfile { Id = id, UserId = 10, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });
        _customerProfileRepository.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(new CustomerProfile { Id = 1, UserId = 10, CustomerType = CustomerType.Retail, Region = "Хатлон", District = "Бохтар" });
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new FarmerProfile { Id = id, UserId = 20, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified });
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(20)).ReturnsAsync(new FarmerProfile { Id = 1, UserId = 20, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified });
        _conversationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static Conversation CreateConversation(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        CustomerId = 10,
        FarmerId = 20,
        IsClosed = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateConversationDto ValidCreateDto(int orderId = 1) => new()
    {
        OrderId = orderId,
        CustomerId = 10,
        FarmerId = 20,
        IsClosed = false
    };

    private static UpdateConversationDto ValidUpdateDto(int id = 1, int orderId = 1) => new()
    {
        Id = id,
        OrderId = orderId,
        CustomerId = 10,
        FarmerId = 20,
        IsClosed = false
    };

    // OrderId = null — чат до заказа (вопрос фермеру про товар).
    private static CreateConversationDto ValidPreOrderCreateDto() => new()
    {
        OrderId = null,
        CustomerId = 10,
        FarmerId = 20,
        IsClosed = false
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ConversationsExist_ReturnsMappedDtos()
    {
        _conversationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateConversation(1), CreateConversation(2, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _conversationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _conversationRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var conversation = CreateConversation(5);
        _conversationRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(conversation);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(conversation.Id, result.Data!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Conversation?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsConversationAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroOrderId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.OrderId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroCustomerId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CustomerId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroFarmerId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FarmerId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CustomerIdMismatch_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.CustomerId = 999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FarmerIdMismatch_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FarmerId = 999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ConversationAlreadyExistsForOrder_ReturnsConflict()
    {
        _conversationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateConversation(1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync: чат до заказа (OrderId = null) ----------

    [Fact]
    public async Task CreateAsync_NullOrderId_ValidData_AddsConversationAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidPreOrderCreateDto());

        Assert.True(result.IsSuccess);
        _conversationRepository.Verify(r => r.AddAsync(It.Is<Conversation>(c => c.OrderId == null)), Times.Once);
        _orderRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullOrderId_CustomerNotFound_ReturnsValidationError()
    {
        _customerProfileRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync((CustomerProfile?)null);

        var result = await _service.CreateAsync(ValidPreOrderCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullOrderId_FarmerNotVerified_ReturnsValidationError()
    {
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(20)).ReturnsAsync(new FarmerProfile
        {
            Id = 1, UserId = 20, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Pending
        });

        var result = await _service.CreateAsync(ValidPreOrderCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullOrderId_ConversationAlreadyExistsForPair_ReturnsConflict()
    {
        _conversationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([new Conversation
        {
            Id = 1, OrderId = null, CustomerId = 10, FarmerId = 20, IsClosed = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        }]);

        var result = await _service.CreateAsync(ValidPreOrderCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullOrderId_ExistingConversationForSameOrderDoesNotBlock()
    {
        // Чат по конкретному заказу и чат "до заказа" — разные ключи
        // уникальности (OrderId vs пара CustomerId/FarmerId), поэтому
        // существование одного не должно мешать создать другой.
        _conversationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateConversation(1, 1)]);

        var result = await _service.CreateAsync(ValidPreOrderCreateDto());

        Assert.True(result.IsSuccess);
        _conversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Once);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesConversationAndReturnsOk()
    {
        var conversation = CreateConversation(1);
        _conversationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(conversation);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _conversationRepository.Verify(r => r.UpdateAsync(It.IsAny<Conversation>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ConversationNotFound_ReturnsNotFound()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Conversation?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _conversationRepository.Verify(r => r.UpdateAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OrderNotFound_ReturnsNotFound()
    {
        var conversation = CreateConversation(1);
        _conversationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(conversation);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _conversationRepository.Verify(r => r.UpdateAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ConversationAlreadyExistsOnAnotherOrder_ReturnsConflict()
    {
        var conversation = CreateConversation(1, 1);
        var other = CreateConversation(2, 2);
        _conversationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(conversation);
        _conversationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([conversation, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _conversationRepository.Verify(r => r.UpdateAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingConversation_DeletesAndReturnsOk()
    {
        var conversation = CreateConversation(1);
        _conversationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(conversation);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _conversationRepository.Verify(r => r.DeleteAsync(conversation), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ConversationNotFound_ReturnsNotFound()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Conversation?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _conversationRepository.Verify(r => r.DeleteAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
