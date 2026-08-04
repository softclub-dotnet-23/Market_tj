using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class CourierDocumentServiceTests
{
    private readonly Mock<ICourierDocumentRepository> _courierDocumentRepository = new();
    private readonly Mock<ICourierProfileRepository> _courierProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<ILogger<CourierDocumentService>> _logger = new();
    private readonly CourierDocumentService _service;

    public CourierDocumentServiceTests()
    {
        _service = new CourierDocumentService(
            _courierDocumentRepository.Object, _courierProfileRepository.Object, _userRepository.Object,
            _auditLogService.Object, _currentUser.Object, _fileStorageService.Object, _logger.Object);

        // Дефолтный документ — CourierProfileId=1 (UserId=1); залогинены как этот курьер.
        _currentUser.Setup(c => c.UserId).Returns(1);
        _currentUser.Setup(c => c.Role).Returns(nameof(UserRole.Courier));
        _courierProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new CourierProfile { Id = id, UserId = 1, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(new CourierProfile { Id = 1, UserId = 1, TransportType = "Car", VehicleNumber = "1", Region = "Хатлон", District = "Бохтар" });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Admin, FullName = "Admin", Email = "a@e.com", PhoneNumber = "1", PasswordHash = "h" });
    }

    private static CourierDocument CreateDocument(int id = 1, int courierProfileId = 1, CourierDocumentType type = CourierDocumentType.DriverLicense, DocumentReviewStatus status = DocumentReviewStatus.Pending) => new()
    {
        Id = id,
        CourierProfileId = courierProfileId,
        DocumentType = type,
        FileUrl = "doc.jpg",
        Status = status,
        UploadedAt = DateTime.UtcNow
    };

    // ---------- UploadAsync ----------

    [Fact]
    public async Task UploadAsync_ValidFile_SavesAndCreatesPendingDocument()
    {
        _fileStorageService.Setup(f => f.SaveAsync(It.IsAny<Stream>(), "license.jpg", "courier-documents/1")).ReturnsAsync("/uploads/courier-documents/1/license.jpg");
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await _service.UploadAsync(1, CourierDocumentType.DriverLicense, stream, "license.jpg", 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentReviewStatus.Pending, result.Data!.Status);
        Assert.Equal("/uploads/courier-documents/1/license.jpg", result.Data.FileUrl);
        _courierDocumentRepository.Verify(r => r.AddAsync(It.Is<CourierDocument>(d => d.CourierProfileId == 1 && d.Status == DocumentReviewStatus.Pending)), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_InvalidExtension_ReturnsValidationErrorWithoutSaving()
    {
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await _service.UploadAsync(1, CourierDocumentType.DriverLicense, stream, "license.exe", 3);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _courierDocumentRepository.Verify(r => r.AddAsync(It.IsAny<CourierDocument>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_CourierProfileNotFound_ReturnsNotFound()
    {
        _courierProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CourierProfile?)null);
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await _service.UploadAsync(999, CourierDocumentType.DriverLicense, stream, "license.jpg", 3);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UploadAsync_NotOwner_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(2);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(2)).ReturnsAsync((CourierProfile?)null);
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await _service.UploadAsync(1, CourierDocumentType.DriverLicense, stream, "license.jpg", 3);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _fileStorageService.Verify(f => f.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_Owner_Succeeds()
    {
        _courierDocumentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDocument());

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _courierDocumentRepository.Verify(r => r.DeleteAsync(It.IsAny<CourierDocument>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(2);
        _courierProfileRepository.Setup(r => r.GetByUserIdAsync(2)).ReturnsAsync((CourierProfile?)null);
        _courierDocumentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateDocument());

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    // ---------- GetPagedAsync (admin) ----------

    [Fact]
    public async Task GetPagedAsync_FilterByStatus_ReturnsOnlyMatching()
    {
        _courierDocumentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([
            CreateDocument(1, status: DocumentReviewStatus.Pending),
            CreateDocument(2, status: DocumentReviewStatus.Approved),
        ]);
        _courierProfileRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetPagedAsync(new PagedRequest { PageNumber = 1, PageSize = 10 }, DocumentReviewStatus.Approved);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal(2, item.Id);
    }

    // ---------- ReviewAsync ----------

    [Fact]
    public async Task ReviewAsync_Approve_UpdatesDocumentAndWritesAuditLog()
    {
        var document = CreateDocument();
        _courierDocumentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(document);

        var result = await _service.ReviewAsync(1, new ReviewCourierDocumentDto { Status = DocumentReviewStatus.Approved }, adminId: 9);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentReviewStatus.Approved, document.Status);
        Assert.Equal(9, document.ReviewedByAdminId);
        _auditLogService.Verify(a => a.CreateAsync(It.IsAny<MarketTJ.Application.Dto.AuditLogDto.CreateAuditLogDto>()), Times.Once);
    }

    [Fact]
    public async Task ReviewAsync_RejectWithoutReason_ReturnsValidationError()
    {
        var document = CreateDocument();
        _courierDocumentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(document);

        var result = await _service.ReviewAsync(1, new ReviewCourierDocumentDto { Status = DocumentReviewStatus.Rejected }, adminId: 9);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ReviewAsync_AlreadyReviewed_ReturnsValidationError()
    {
        var document = CreateDocument(status: DocumentReviewStatus.Approved);
        _courierDocumentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(document);

        var result = await _service.ReviewAsync(1, new ReviewCourierDocumentDto { Status = DocumentReviewStatus.Rejected, RejectionReason = "x" }, adminId: 9);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // ---------- HasApprovedRequiredDocumentsAsync ----------

    [Fact]
    public async Task HasApprovedRequiredDocumentsAsync_BothApproved_ReturnsTrue()
    {
        _courierDocumentRepository.Setup(r => r.GetByCourierProfileIdAsync(1)).ReturnsAsync([
            CreateDocument(1, type: CourierDocumentType.DriverLicense, status: DocumentReviewStatus.Approved),
            CreateDocument(2, type: CourierDocumentType.VehicleRegistration, status: DocumentReviewStatus.Approved),
        ]);

        Assert.True(await _service.HasApprovedRequiredDocumentsAsync(1));
    }

    [Fact]
    public async Task HasApprovedRequiredDocumentsAsync_OneMissing_ReturnsFalse()
    {
        _courierDocumentRepository.Setup(r => r.GetByCourierProfileIdAsync(1)).ReturnsAsync([
            CreateDocument(1, type: CourierDocumentType.DriverLicense, status: DocumentReviewStatus.Approved),
        ]);

        Assert.False(await _service.HasApprovedRequiredDocumentsAsync(1));
    }

    [Fact]
    public async Task HasApprovedRequiredDocumentsAsync_OnePendingNotApproved_ReturnsFalse()
    {
        // Курьерский гейт строже фермерского — "отправлен" недостаточно, нужен
        // именно Approved (2026-08-04).
        _courierDocumentRepository.Setup(r => r.GetByCourierProfileIdAsync(1)).ReturnsAsync([
            CreateDocument(1, type: CourierDocumentType.DriverLicense, status: DocumentReviewStatus.Approved),
            CreateDocument(2, type: CourierDocumentType.VehicleRegistration, status: DocumentReviewStatus.Pending),
        ]);

        Assert.False(await _service.HasApprovedRequiredDocumentsAsync(1));
    }
}
