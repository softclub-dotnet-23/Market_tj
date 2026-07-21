using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ChatMessageDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class ChatMessageServiceTests
{
    private readonly Mock<IChatMessageRepository> _chatMessageRepository = new();
    private readonly Mock<IConversationRepository> _conversationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<ChatMessageService>> _logger = new();
    private readonly ChatMessageService _service;

    public ChatMessageServiceTests()
    {
        _service = new ChatMessageService(_chatMessageRepository.Object, _conversationRepository.Object, _userRepository.Object, _logger.Object);
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Conversation
        {
            Id = id, OrderId = 1, CustomerId = 10, FarmerId = 20, IsClosed = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
    }

    private static ChatMessage CreateMessage(int id = 1, int conversationId = 1, int senderId = 10) => new()
    {
        Id = id,
        ConversationId = conversationId,
        SenderId = senderId,
        Message = "Здравствуйте",
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateChatMessageDto ValidCreateDto(int conversationId = 1, int senderId = 10) => new()
    {
        ConversationId = conversationId,
        SenderId = senderId,
        Message = "Здравствуйте",
        IsRead = false
    };

    private static UpdateChatMessageDto ValidUpdateDto(int id = 1, int conversationId = 1, int senderId = 10) => new()
    {
        Id = id,
        ConversationId = conversationId,
        SenderId = senderId,
        Message = "Здравствуйте",
        IsRead = false
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_MessagesExist_ReturnsMappedDtos()
    {
        _chatMessageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateMessage(1), CreateMessage(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _chatMessageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _chatMessageRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var message = CreateMessage(5);
        _chatMessageRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(message);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(message.Id, result.Data!.Id);
        Assert.Equal(message.Message, result.Data!.Message);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _chatMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ChatMessage?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _chatMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsMessageAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _chatMessageRepository.Verify(r => r.AddAsync(It.IsAny<ChatMessage>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroConversationId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.ConversationId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _chatMessageRepository.Verify(r => r.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyMessage_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Message = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _chatMessageRepository.Verify(r => r.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ConversationNotFound_ReturnsNotFound()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Conversation?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _chatMessageRepository.Verify(r => r.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SenderNotParticipant_ReturnsUnauthorized()
    {
        var result = await _service.CreateAsync(ValidCreateDto(senderId: 999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _chatMessageRepository.Verify(r => r.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ClosedConversation_ReturnsValidationError()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Conversation
        {
            Id = 1, OrderId = 1, CustomerId = 10, FarmerId = 20, IsClosed = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _chatMessageRepository.Verify(r => r.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SenderNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _chatMessageRepository.Verify(r => r.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UpdatesConversationUpdatedAt()
    {
        var conversation = new Conversation { Id = 1, OrderId = 1, CustomerId = 10, FarmerId = 20, IsClosed = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        _conversationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(conversation);
        var before = conversation.UpdatedAt;

        await _service.CreateAsync(ValidCreateDto());

        Assert.True(conversation.UpdatedAt > before);
        _conversationRepository.Verify(r => r.UpdateAsync(conversation), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesMessageAndReturnsOk()
    {
        var message = CreateMessage(1);
        _chatMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _chatMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<ChatMessage>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_MessageNotFound_ReturnsNotFound()
    {
        _chatMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ChatMessage?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _chatMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ConversationNotFound_ReturnsNotFound()
    {
        var message = CreateMessage(1);
        _chatMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);
        _conversationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Conversation?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _chatMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SenderNotParticipant_ReturnsUnauthorized()
    {
        var message = CreateMessage(1);
        _chatMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, senderId: 999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _chatMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SenderNotFound_ReturnsNotFound()
    {
        var message = CreateMessage(1);
        _chatMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _chatMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _chatMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingMessage_DeletesAndReturnsOk()
    {
        var message = CreateMessage(1);
        _chatMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _chatMessageRepository.Verify(r => r.DeleteAsync(message), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MessageNotFound_ReturnsNotFound()
    {
        _chatMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ChatMessage?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _chatMessageRepository.Verify(r => r.DeleteAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _chatMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
