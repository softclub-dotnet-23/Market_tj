using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Dto.CourierDocumentDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Interfaces.Services;

public interface ICourierDocumentService
{
    Task<Result<IEnumerable<GetCourierDocumentDto>>> GetAllAsync();
    Task<Result<GetCourierDocumentDto>> UploadAsync(int courierProfileId, CourierDocumentType documentType, Stream fileContent, string fileName, long fileSizeBytes);
    Task<Result<string>> DeleteAsync(int id);

    Task<Result<PagedResult<GetAdminCourierDocumentDto>>> GetPagedAsync(PagedRequest request, DocumentReviewStatus? status);
    Task<Result<string>> ReviewAsync(int id, ReviewCourierDocumentDto dto, int adminId);

    // Гейт "курьер не может стать isAvailable, пока оба обязательных
    // документа не одобрены" — по прямому запросу пользователя (2026-08-04),
    // сознательно строже фермерского RequiredDocumentTypes-гейта в
    // ProductListingService (там достаточно "не отклонён", здесь нужно
    // именно Approved). Используется из CourierProfileService.
    Task<bool> HasApprovedRequiredDocumentsAsync(int courierProfileId);
}
