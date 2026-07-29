using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerDocumentDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class FarmerDocumentService(
    IFarmerDocumentRepository farmerDocumentRepository,
    IFarmerProfileRepository farmerProfileRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    ILogger<FarmerDocumentService> logger) : IFarmerDocumentService
{
    // Audit 2026-07-28, находка 2.2 (IDOR): FarmerDocument.FarmerProfileId —
    // Id профиля фермера, владение резолвится через него (документы верификации —
    // чувствительные данные, не должны быть видны другим фермерам).
    private async Task<bool> IsOwnerAsync(int farmerProfileId)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;

        var myProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return myProfile is not null && myProfile.Id == farmerProfileId;
    }

    public async Task<Result<IEnumerable<GetFarmerDocumentDto>>> GetAllAsync()
    {
        try
        {
            var documents = await farmerDocumentRepository.GetAllAsync();

            if (!currentUser.IsAdmin())
            {
                var myProfile = currentUser.UserId is null ? null : await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                documents = myProfile is null ? [] : documents.Where(d => d.FarmerProfileId == myProfile.Id).ToList();
            }

            return Result<IEnumerable<GetFarmerDocumentDto>>.Ok(documents.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка документов фермеров");
            return Result<IEnumerable<GetFarmerDocumentDto>>.Fail("Не удалось получить список документов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetFarmerDocumentDto?>> GetByIdAsync(int id)
    {
        try
        {
            var document = await farmerDocumentRepository.GetByIdAsync(id);
            if (document is null)
                return Result<GetFarmerDocumentDto?>.Fail("Документ не найден", ErrorType.NotFound);

            if (!await IsOwnerAsync(document.FarmerProfileId))
                return Result<GetFarmerDocumentDto?>.Fail("Нет доступа к этому документу", ErrorType.Forbidden);

            return Result<GetFarmerDocumentDto?>.Ok(ToGetDto(document));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении документа {Id}", id);
            return Result<GetFarmerDocumentDto?>.Fail("Не удалось получить документ", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateFarmerDocumentDto dto)
    {
        try
        {
            var validation = FarmerDocumentValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerProfileId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            if (!await IsOwnerAsync(dto.FarmerProfileId))
                return Result<string>.Fail("Нельзя загрузить документ для чужого профиля фермера", ErrorType.Forbidden);

            if (dto.ReviewedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.ReviewedByAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("ReviewedByAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            var document = new FarmerDocument
            {
                FarmerProfileId = dto.FarmerProfileId,
                DocumentType = dto.DocumentType,
                FileUrl = dto.FileUrl,
                Status = dto.Status,
                UploadedAt = dto.UploadedAt == default ? DateTime.UtcNow : dto.UploadedAt,
                ReviewedAt = dto.ReviewedAt,
                ReviewedByAdminId = dto.ReviewedByAdminId,
                RejectionReason = dto.RejectionReason
            };

            await farmerDocumentRepository.AddAsync(document);
            return Result<string>.Ok("Документ создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании документа");
            return Result<string>.Fail("Не удалось создать документ", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateFarmerDocumentDto dto)
    {
        try
        {
            var validation = FarmerDocumentValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var document = await farmerDocumentRepository.GetByIdAsync(id);
            if (document is null)
                return Result<string>.Fail("Документ не найден", ErrorType.NotFound);

            if (!await IsOwnerAsync(document.FarmerProfileId))
                return Result<string>.Fail("Нет доступа к этому документу", ErrorType.Forbidden);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerProfileId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            if (dto.ReviewedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.ReviewedByAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("ReviewedByAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            document.FarmerProfileId = dto.FarmerProfileId;
            document.DocumentType = dto.DocumentType;
            document.FileUrl = dto.FileUrl;
            document.Status = dto.Status;
            document.UploadedAt = dto.UploadedAt;
            document.ReviewedAt = dto.ReviewedAt;
            document.ReviewedByAdminId = dto.ReviewedByAdminId;
            document.RejectionReason = dto.RejectionReason;

            await farmerDocumentRepository.UpdateAsync(document);
            return Result<string>.Ok("Документ обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении документа {Id}", id);
            return Result<string>.Fail("Не удалось обновить документ", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var document = await farmerDocumentRepository.GetByIdAsync(id);
            if (document is null)
                return Result<string>.Fail("Документ не найден", ErrorType.NotFound);

            if (!await IsOwnerAsync(document.FarmerProfileId))
                return Result<string>.Fail("Нет доступа к этому документу", ErrorType.Forbidden);

            await farmerDocumentRepository.DeleteAsync(document);
            return Result<string>.Ok("Документ удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении документа {Id}", id);
            return Result<string>.Fail("Не удалось удалить документ", ErrorType.InternalServerError);
        }
    }

    private static GetFarmerDocumentDto ToGetDto(FarmerDocument document) => new()
    {
        Id = document.Id,
        FarmerProfileId = document.FarmerProfileId,
        DocumentType = document.DocumentType,
        FileUrl = document.FileUrl,
        Status = document.Status,
        UploadedAt = document.UploadedAt,
        ReviewedAt = document.ReviewedAt,
        ReviewedByAdminId = document.ReviewedByAdminId,
        RejectionReason = document.RejectionReason
    };
}
