using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.FarmerDocumentDto;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Interfaces.Services;

public interface IFarmerDocumentService
{
    Task<Result<IEnumerable<GetFarmerDocumentDto>>> GetAllAsync();
    Task<Result<GetFarmerDocumentDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateFarmerDocumentDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateFarmerDocumentDto dto);
    Task<Result<string>> DeleteAsync(int id);

    // Дополнено по явному запросу пользователя — раньше документ можно было
    // создать только с уже готовым FileUrl (см. CreateAsync), реального
    // способа загрузить файл не было (см. history: "documents.uploadHint").
    // Новая запись всегда создаётся со Status = Pending, ReviewedAt/
    // ReviewedByAdminId = null — как и положено свежезагруженному документу.
    Task<Result<GetFarmerDocumentDto>> UploadAsync(int farmerProfileId, FarmerDocumentType documentType, Stream fileContent, string fileName, long fileSizeBytes);

    // Admin-модерация загруженных документов (паспорт и т.д.) — раньше
    // фермер мог загрузить документ, но админ его нигде не видел и не мог
    // одобрить/отклонить (см. Admin/AdminFarmerDocumentController).
    Task<Result<PagedResult<GetAdminFarmerDocumentDto>>> GetPagedAsync(PagedRequest request, DocumentReviewStatus? status);
    Task<Result<string>> ReviewAsync(int id, ReviewFarmerDocumentDto dto, int adminId);
}
