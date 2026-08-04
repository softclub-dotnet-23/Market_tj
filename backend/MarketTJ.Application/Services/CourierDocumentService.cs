using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Dto.AuditLogDto;
using MarketTJ.Application.Dto.CourierDocumentDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Зеркалит FarmerDocumentService — тот же паттерн (IDOR-проверка через
// профиль, upload через IFileStorageService, один сервис на курьера и
// админа). См. комментарии там для обоснования решений, которые здесь не
// повторяются дословно.
public class CourierDocumentService(
    ICourierDocumentRepository courierDocumentRepository,
    ICourierProfileRepository courierProfileRepository,
    IUserRepository userRepository,
    IAuditLogService auditLogService,
    ICurrentUserService currentUser,
    IFileStorageService fileStorageService,
    ILogger<CourierDocumentService> logger) : ICourierDocumentService
{
    private static readonly CourierDocumentType[] RequiredDocumentTypes =
        [CourierDocumentType.DriverLicense, CourierDocumentType.VehicleRegistration];

    private async Task<bool> IsOwnerAsync(int courierProfileId)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;

        var myProfile = await courierProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return myProfile is not null && myProfile.Id == courierProfileId;
    }

    public async Task<Result<IEnumerable<GetCourierDocumentDto>>> GetAllAsync()
    {
        try
        {
            var documents = await courierDocumentRepository.GetAllAsync();

            if (!currentUser.IsAdmin())
            {
                var myProfile = currentUser.UserId is null ? null : await courierProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                documents = myProfile is null ? [] : documents.Where(d => d.CourierProfileId == myProfile.Id).ToList();
            }

            return Result<IEnumerable<GetCourierDocumentDto>>.Ok(documents.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка документов курьеров");
            return Result<IEnumerable<GetCourierDocumentDto>>.Fail("Не удалось получить список документов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetCourierDocumentDto>> UploadAsync(int courierProfileId, CourierDocumentType documentType, Stream fileContent, string fileName, long fileSizeBytes)
    {
        try
        {
            var fileValidation = FileUploadValidator.ValidateDocument(fileName, fileSizeBytes);
            if (fileValidation is not null)
                return Result<GetCourierDocumentDto>.Fail(fileValidation.Error!, fileValidation.ErrorType!.Value);

            var courierProfile = await courierProfileRepository.GetByIdAsync(courierProfileId);
            if (courierProfile is null)
                return Result<GetCourierDocumentDto>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            if (!await IsOwnerAsync(courierProfileId))
                return Result<GetCourierDocumentDto>.Fail("Нельзя загрузить документ для чужого профиля курьера", ErrorType.Forbidden);

            var fileUrl = await fileStorageService.SaveAsync(fileContent, fileName, $"courier-documents/{courierProfileId}");

            var document = new CourierDocument
            {
                CourierProfileId = courierProfileId,
                DocumentType = documentType,
                FileUrl = fileUrl,
                Status = DocumentReviewStatus.Pending,
                UploadedAt = DateTime.UtcNow
            };

            await courierDocumentRepository.AddAsync(document);
            return Result<GetCourierDocumentDto>.Ok(ToGetDto(document));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке документа курьера {CourierProfileId}", courierProfileId);
            return Result<GetCourierDocumentDto>.Fail("Не удалось загрузить документ", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var document = await courierDocumentRepository.GetByIdAsync(id);
            if (document is null)
                return Result<string>.Fail("Документ не найден", ErrorType.NotFound);

            if (!await IsOwnerAsync(document.CourierProfileId))
                return Result<string>.Fail("Нет доступа к этому документу", ErrorType.Forbidden);

            await courierDocumentRepository.DeleteAsync(document);
            return Result<string>.Ok("Документ удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении документа {Id}", id);
            return Result<string>.Fail("Не удалось удалить документ", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<PagedResult<GetAdminCourierDocumentDto>>> GetPagedAsync(PagedRequest request, DocumentReviewStatus? status)
    {
        try
        {
            var all = await courierDocumentRepository.GetAllAsync();

            IEnumerable<CourierDocument> filtered = all;
            if (status is not null)
                filtered = filtered.Where(d => d.Status == status);

            filtered = request.SortDescending
                ? filtered.OrderByDescending(d => d.UploadedAt)
                : filtered.OrderBy(d => d.UploadedAt);

            var materialized = filtered.ToList();

            var courierProfiles = (await courierProfileRepository.GetAllAsync()).ToDictionary(c => c.Id, c => c);
            var courierUserIds = courierProfiles.Values.Select(c => c.UserId).Distinct().ToHashSet();
            var users = (await userRepository.GetAllAsync())
                .Where(u => courierUserIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u);

            var page = materialized
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(d => ToAdminGetDto(d, courierProfiles, users))
                .ToList();

            return Result<PagedResult<GetAdminCourierDocumentDto>>.Ok(
                PagedResult<GetAdminCourierDocumentDto>.Ok(page, materialized.Count, request.PageNumber, request.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка документов курьеров (paged)");
            return Result<PagedResult<GetAdminCourierDocumentDto>>.Fail("Не удалось получить список документов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> ReviewAsync(int id, ReviewCourierDocumentDto dto, int adminId)
    {
        try
        {
            if (dto.Status is not (DocumentReviewStatus.Approved or DocumentReviewStatus.Rejected))
                return Result<string>.Fail("Рассмотреть документ можно только в статус Approved или Rejected", ErrorType.Validation);

            if (dto.Status == DocumentReviewStatus.Rejected && string.IsNullOrWhiteSpace(dto.RejectionReason))
                return Result<string>.Fail("При отклонении документа причина обязательна", ErrorType.Validation);

            var document = await courierDocumentRepository.GetByIdAsync(id);
            if (document is null)
                return Result<string>.Fail("Документ не найден", ErrorType.NotFound);

            if (document.Status != DocumentReviewStatus.Pending)
                return Result<string>.Fail("Рассмотреть можно только документ в статусе Pending", ErrorType.Validation);

            document.Status = dto.Status;
            document.ReviewedAt = DateTime.UtcNow;
            document.ReviewedByAdminId = adminId;
            document.RejectionReason = dto.Status == DocumentReviewStatus.Rejected ? dto.RejectionReason : null;

            await courierDocumentRepository.UpdateAsync(document);

            await auditLogService.CreateAsync(new CreateAuditLogDto
            {
                AdminId = adminId,
                Action = "ReviewCourierDocument",
                EntityType = nameof(CourierDocument),
                EntityId = id,
                Details = $"Документ рассмотрен, статус: {dto.Status}"
            });

            return Result<string>.Ok("Документ рассмотрен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при рассмотрении документа {Id}", id);
            return Result<string>.Fail("Не удалось рассмотреть документ", ErrorType.InternalServerError);
        }
    }

    public async Task<bool> HasApprovedRequiredDocumentsAsync(int courierProfileId)
    {
        var documents = await courierDocumentRepository.GetByCourierProfileIdAsync(courierProfileId);
        var approvedTypes = documents
            .Where(d => d.Status == DocumentReviewStatus.Approved)
            .Select(d => d.DocumentType)
            .ToHashSet();

        return RequiredDocumentTypes.All(approvedTypes.Contains);
    }

    private static GetAdminCourierDocumentDto ToAdminGetDto(CourierDocument document, IReadOnlyDictionary<int, CourierProfile> courierProfiles, IReadOnlyDictionary<int, User> users)
    {
        courierProfiles.TryGetValue(document.CourierProfileId, out var courierProfile);
        User? courierUser = courierProfile is not null && users.TryGetValue(courierProfile.UserId, out var u) ? u : null;

        return new GetAdminCourierDocumentDto
        {
            Id = document.Id,
            CourierProfileId = document.CourierProfileId,
            CourierFullName = courierUser?.FullName,
            CourierPhoneNumber = courierUser?.PhoneNumber,
            DocumentType = document.DocumentType,
            FileUrl = document.FileUrl,
            Status = document.Status,
            UploadedAt = document.UploadedAt,
            ReviewedAt = document.ReviewedAt,
            ReviewedByAdminId = document.ReviewedByAdminId,
            RejectionReason = document.RejectionReason
        };
    }

    private static GetCourierDocumentDto ToGetDto(CourierDocument document) => new()
    {
        Id = document.Id,
        CourierProfileId = document.CourierProfileId,
        DocumentType = document.DocumentType,
        FileUrl = document.FileUrl,
        Status = document.Status,
        UploadedAt = document.UploadedAt,
        ReviewedAt = document.ReviewedAt,
        ReviewedByAdminId = document.ReviewedByAdminId,
        RejectionReason = document.RejectionReason
    };
}
