using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerDocumentDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class FarmerDocumentServiceTests
{
    private readonly Mock<IFarmerDocumentRepository> _farmerDocumentRepository = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<FarmerDocumentService>> _logger = new();
    private readonly FarmerDocumentService _service;

    public FarmerDocumentServiceTests()
    {
        _service = new FarmerDocumentService(_farmerDocumentRepository.Object, _farmerProfileRepository.Object, _userRepository.Object, _logger.Object);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new FarmerProfile { Id = id, UserId = 1, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Pending });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Admin, FullName = "Admin", Email = "a@e.com", PhoneNumber = "1", PasswordHash = "h" });
    }

    private static FarmerDocument CreateDocument(int id = 1, int farmerProfileId = 1) => new()
    {
        Id = id,
        FarmerProfileId = farmerProfileId,
        DocumentType = FarmerDocumentType.Passport,
        FileUrl = "doc.pdf",
        Status = DocumentReviewStatus.Pending,
        UploadedAt = DateTime.UtcNow
    };

    private static CreateFarmerDocumentDto ValidCreateDto(int farmerProfileId = 1) => new()
    {
        FarmerProfileId = farmerProfileId,
        DocumentType = FarmerDocumentType.Passport,
        FileUrl = "doc.pdf",
        Status = DocumentReviewStatus.Pending,
        UploadedAt = DateTime.UtcNow
    };

    private static UpdateFarmerDocumentDto ValidUpdateDto(int id = 1, int farmerProfileId = 1) => new()
    {
        Id = id,
        FarmerProfileId = farmerProfileId,
        DocumentType = FarmerDocumentType.Passport,
        FileUrl = "doc.pdf",
        Status = DocumentReviewStatus.Pending,
        UploadedAt = DateTime.UtcNow
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_DocumentsExist_ReturnsMappedDtos()
    {
        _farmerDocumentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateDocument(1), CreateDocument(2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _farmerDocumentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerDocumentRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var document = CreateDocument(5);
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(document);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(document.Id, result.Data!.Id);
        Assert.Equal(document.FileUrl, result.Data!.FileUrl);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerDocument?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsDocumentAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroFarmerProfileId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FarmerProfileId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidDocumentType_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.DocumentType = (FarmerDocumentType)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptyFileUrl_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FileUrl = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidStatus_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = (DocumentReviewStatus)999;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RejectedWithoutRejectionReason_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Status = DocumentReviewStatus.Rejected;
        dto.RejectionReason = null;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FarmerProfileNotFound_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReviewedByAdminNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);
        var dto = ValidCreateDto();
        dto.ReviewedByAdminId = 5;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReviewedByAdminNotAdminRole_ReturnsValidationError()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { Id = 5, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
        var dto = ValidCreateDto();
        dto.ReviewedByAdminId = 5;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.AddAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesDocumentAndReturnsOk()
    {
        var document = CreateDocument(1);
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(document);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _farmerDocumentRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerDocument>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DocumentNotFound_ReturnsNotFound()
    {
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerDocument?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RejectedWithoutRejectionReason_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Status = DocumentReviewStatus.Rejected;
        dto.RejectionReason = null;

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_FarmerProfileNotFound_ReturnsNotFound()
    {
        var document = CreateDocument(1);
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(document);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingDocument_DeletesAndReturnsOk()
    {
        var document = CreateDocument(1);
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(document);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _farmerDocumentRepository.Verify(r => r.DeleteAsync(document), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DocumentNotFound_ReturnsNotFound()
    {
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerDocument?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerDocumentRepository.Verify(r => r.DeleteAsync(It.IsAny<FarmerDocument>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerDocumentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
