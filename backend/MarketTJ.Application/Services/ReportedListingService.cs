using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ReportedListingDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class ReportedListingService(
    IReportedListingRepository reportedListingRepository,
    IProductListingRepository productListingRepository,
    IUserRepository userRepository,
    ILogger<ReportedListingService> logger) : IReportedListingService
{
    public async Task<Result<IEnumerable<GetReportedListingDto>>> GetAllAsync()
    {
        try
        {
            var reports = await reportedListingRepository.GetAllAsync();
            return Result<IEnumerable<GetReportedListingDto>>.Ok(reports.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка жалоб на объявления");
            return Result<IEnumerable<GetReportedListingDto>>.Fail("Не удалось получить список жалоб", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetReportedListingDto?>> GetByIdAsync(int id)
    {
        try
        {
            var report = await reportedListingRepository.GetByIdAsync(id);
            if (report is null)
                return Result<GetReportedListingDto?>.Fail("Жалоба не найдена", ErrorType.NotFound);

            return Result<GetReportedListingDto?>.Ok(ToGetDto(report));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении жалобы {Id}", id);
            return Result<GetReportedListingDto?>.Fail("Не удалось получить жалобу", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateReportedListingDto dto)
    {
        try
        {
            var validation = ReportedListingValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            var reportedByUser = await userRepository.GetByIdAsync(dto.ReportedByUserId);
            if (reportedByUser is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (dto.ReviewedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.ReviewedByAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("ReviewedByAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            var report = new ReportedListing
            {
                ProductListingId = dto.ProductListingId,
                ReportedByUserId = dto.ReportedByUserId,
                Reason = dto.Reason,
                Comment = dto.Comment,
                Status = dto.Status,
                ReviewedAt = dto.ReviewedAt,
                ReviewedByAdminId = dto.ReviewedByAdminId,
                CreatedAt = DateTime.UtcNow
            };

            await reportedListingRepository.AddAsync(report);
            return Result<string>.Ok("Жалоба создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании жалобы");
            return Result<string>.Fail("Не удалось создать жалобу", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateReportedListingDto dto)
    {
        try
        {
            var validation = ReportedListingValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var report = await reportedListingRepository.GetByIdAsync(id);
            if (report is null)
                return Result<string>.Fail("Жалоба не найдена", ErrorType.NotFound);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            var reportedByUser = await userRepository.GetByIdAsync(dto.ReportedByUserId);
            if (reportedByUser is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (dto.ReviewedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.ReviewedByAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("ReviewedByAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            report.ProductListingId = dto.ProductListingId;
            report.ReportedByUserId = dto.ReportedByUserId;
            report.Reason = dto.Reason;
            report.Comment = dto.Comment;
            report.Status = dto.Status;
            report.ReviewedAt = dto.ReviewedAt;
            report.ReviewedByAdminId = dto.ReviewedByAdminId;

            await reportedListingRepository.UpdateAsync(report);
            return Result<string>.Ok("Жалоба обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении жалобы {Id}", id);
            return Result<string>.Fail("Не удалось обновить жалобу", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var report = await reportedListingRepository.GetByIdAsync(id);
            if (report is null)
                return Result<string>.Fail("Жалоба не найдена", ErrorType.NotFound);

            await reportedListingRepository.DeleteAsync(report);
            return Result<string>.Ok("Жалоба удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении жалобы {Id}", id);
            return Result<string>.Fail("Не удалось удалить жалобу", ErrorType.InternalServerError);
        }
    }

    private static GetReportedListingDto ToGetDto(ReportedListing report) => new()
    {
        Id = report.Id,
        ProductListingId = report.ProductListingId,
        ReportedByUserId = report.ReportedByUserId,
        Reason = report.Reason,
        Comment = report.Comment,
        Status = report.Status,
        CreatedAt = report.CreatedAt,
        ReviewedAt = report.ReviewedAt,
        ReviewedByAdminId = report.ReviewedByAdminId
    };
}
