using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.SupportMessageDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class SupportMessageServiceTests
{
    private readonly Mock<ISupportMessageRepository> _supportMessageRepository = new();
    private readonly Mock<ISupportTicketRepository> _supportTicketRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<SupportMessageService>> _logger = new();
    private readonly SupportMessageService _service;

    public SupportMessageServiceTests()
    {
        _service = new SupportMessageService(_supportMessageRepository.Object, _supportTicketRepository.Object, _userRepository.Object, _logger.Object);
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new SupportTicket
        {
            Id = id, UserId = 1, Subject = "Проблема", Status = SupportTicketStatus.Open, Priority = SupportPriority.Normal, CreatedAt = DateTime.UtcNow
        });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
    }

    private static SupportMessage CreateMessage(int id = 1, int ticketId = 1) => new()
    {
        Id = id,
        SupportTicketId = ticketId,
        SenderId = 1,
        Message = "Здравствуйте, у меня проблема",
        CreatedAt = DateTime.UtcNow
    };

    private static CreateSupportMessageDto ValidCreateDto(int ticketId = 1) => new()
    {
        SupportTicketId = ticketId,
        SenderId = 1,
        Message = "Здравствуйте, у меня проблема"
    };

    private static UpdateSupportMessageDto ValidUpdateDto(int id = 1, int ticketId = 1) => new()
    {
        Id = id,
        SupportTicketId = ticketId,
        SenderId = 1,
        Message = "Здравствуйте, у меня проблема"
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_MessagesExist_ReturnsMappedDtos()
    {
        _supportMessageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateMessage(1), CreateMessage(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _supportMessageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportMessageRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var message = CreateMessage(5);
        _supportMessageRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(message);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(message.Id, result.Data!.Id);
        Assert.Equal(message.Message, result.Data!.Message);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _supportMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SupportMessage?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

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
        _supportMessageRepository.Verify(r => r.AddAsync(It.IsAny<SupportMessage>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroSupportTicketId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.SupportTicketId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportMessageRepository.Verify(r => r.AddAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroSenderId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.SenderId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportMessageRepository.Verify(r => r.AddAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyMessage_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Message = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportMessageRepository.Verify(r => r.AddAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_TicketNotFound_ReturnsNotFound()
    {
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SupportTicket?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportMessageRepository.Verify(r => r.AddAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SenderNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportMessageRepository.Verify(r => r.AddAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesMessageAndReturnsOk()
    {
        var message = CreateMessage(1);
        _supportMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _supportMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportMessage>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_MessageNotFound_ReturnsNotFound()
    {
        _supportMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SupportMessage?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_TicketNotFound_ReturnsNotFound()
    {
        var message = CreateMessage(1);
        _supportMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SupportTicket?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SenderNotFound_ReturnsNotFound()
    {
        var message = CreateMessage(1);
        _supportMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportMessageRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingMessage_DeletesAndReturnsOk()
    {
        var message = CreateMessage(1);
        _supportMessageRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(message);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _supportMessageRepository.Verify(r => r.DeleteAsync(message), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MessageNotFound_ReturnsNotFound()
    {
        _supportMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SupportMessage?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportMessageRepository.Verify(r => r.DeleteAsync(It.IsAny<SupportMessage>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportMessageRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
