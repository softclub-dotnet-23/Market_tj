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
    ILogger<FarmerDocumentService> logger) : IFarmerDocumentService
{
    public async Task<Result<IEnumerable<GetFarmerDocumentDto>>> GetAllAsync()
    {
        try
        {
            var documents = await farmerDocumentRepository.GetAllAsync();
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
