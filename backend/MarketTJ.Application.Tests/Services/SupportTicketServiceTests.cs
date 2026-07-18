using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.SupportTicketDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class SupportTicketServiceTests
{
    private readonly Mock<ISupportTicketRepository> _supportTicketRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<SupportTicketService>> _logger = new();
    private readonly SupportTicketService _service;

    public SupportTicketServiceTests()
    {
        _service = new SupportTicketService(_supportTicketRepository.Object, _userRepository.Object, _logger.Object);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Admin, FullName = "Admin", Email = "a@e.com", PhoneNumber = "1", PasswordHash = "h" });
    }

    private static SupportTicket CreateTicket(int id = 1, int userId = 1) => new()
    {
        Id = id,
        UserId = userId,
        Subject = "Проблема с заказом",
        Status = SupportTicketStatus.Open,
        Priority = SupportPriority.Normal,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateSupportTicketDto ValidCreateDto(int userId = 1) => new()
    {
        UserId = userId,
        Subject = "Проблема с заказом",
        Status = SupportTicketStatus.Open,
        Priority = SupportPriority.Normal
    };

    private static UpdateSupportTicketDto ValidUpdateDto(int id = 1, int userId = 1) => new()
    {
        Id = id,
        UserId = userId,
        Subject = "Проблема с заказом",
        Status = SupportTicketStatus.Open,
        Priority = SupportPriority.Normal
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_TicketsExist_ReturnsMappedDtos()
    {
        _supportTicketRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateTicket(1), CreateTicket(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _supportTicketRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportTicketRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var ticket = CreateTicket(5);
        _supportTicketRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(ticket);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(ticket.Id, result.Data!.Id);
        Assert.Equal(ticket.Subject, result.Data!.Subject);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SupportTicket?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsTicketAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _supportTicketRepository.Verify(r => r.AddAsync(It.IsAny<SupportTicket>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroUserId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.UserId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportTicketRepository.Verify(r => r.AddAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptySubject_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Subject = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportTicketRepository.Verify(r => r.AddAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = (SupportTicketStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportTicketRepository.Verify(r => r.AddAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidPriority_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Priority = (SupportPriority)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportTicketRepository.Verify(r => r.AddAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportTicketRepository.Verify(r => r.AddAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AssignedToAdminNotAdminRole_ReturnsValidationError()
    {
        _userRepository.SetupSequence(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" })
            .ReturnsAsync(new User { Id = 5, Role = UserRole.Customer, FullName = "U2", Email = "u2@e.com", PhoneNumber = "2", PasswordHash = "h" });
        var dto = ValidCreateDto();
        dto.AssignedToAdminId = 5;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportTicketRepository.Verify(r => r.AddAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesTicketAndReturnsOk()
    {
        var ticket = CreateTicket(1);
        _supportTicketRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _supportTicketRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportTicket>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_TicketNotFound_ReturnsNotFound()
    {
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SupportTicket?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportTicketRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptySubject_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Subject = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _supportTicketRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UserNotFound_ReturnsNotFound()
    {
        var ticket = CreateTicket(1);
        _supportTicketRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportTicketRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingTicket_DeletesAndReturnsOk()
    {
        var ticket = CreateTicket(1);
        _supportTicketRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _supportTicketRepository.Verify(r => r.DeleteAsync(ticket), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_TicketNotFound_ReturnsNotFound()
    {
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SupportTicket?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _supportTicketRepository.Verify(r => r.DeleteAsync(It.IsAny<SupportTicket>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _supportTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
