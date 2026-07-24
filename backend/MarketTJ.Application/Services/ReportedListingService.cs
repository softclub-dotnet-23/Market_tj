using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuditLogDto;
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
    IAuditLogService auditLogService,
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

    public async Task<Result<PagedResult<GetReportedListingDto>>> GetPagedAsync(PagedRequest request, ReportStatus? status)
    {
        try
        {
            var all = await reportedListingRepository.GetAllAsync();

            IEnumerable<ReportedListing> filtered = all;
            if (status is not null)
                filtered = filtered.Where(r => r.Status == status);

            filtered = request.SortDescending
                ? filtered.OrderByDescending(r => r.CreatedAt)
                : filtered.OrderBy(r => r.CreatedAt);

            var materialized = filtered.ToList();
            var page = materialized
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ToGetDto)
                .ToList();

            return Result<PagedResult<GetReportedListingDto>>.Ok(
                PagedResult<GetReportedListingDto>.Ok(page, materialized.Count, request.PageNumber, request.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка жалоб (paged)");
            return Result<PagedResult<GetReportedListingDto>>.Fail("Не удалось получить список жалоб", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> ResolveAsync(int id, ReportStatus resolution, int adminId)
    {
        try
        {
            if (resolution is not (ReportStatus.Reviewed or ReportStatus.Dismissed))
                return Result<string>.Fail("Разрешить жалобу можно только в статус Reviewed или Dismissed", ErrorType.Validation);

            var report = await reportedListingRepository.GetByIdAsync(id);
            if (report is null)
                return Result<string>.Fail("Жалоба не найдена", ErrorType.NotFound);

            if (report.Status != ReportStatus.Pending)
                return Result<string>.Fail("Рассмотреть можно только жалобу в статусе Pending", ErrorType.Validation);

            report.Status = resolution;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedByAdminId = adminId;

            await reportedListingRepository.UpdateAsync(report);

            await auditLogService.CreateAsync(new CreateAuditLogDto
            {
                AdminId = adminId,
                Action = "ResolveReportedListing",
                EntityType = nameof(ReportedListing),
                EntityId = id,
                Details = $"Жалоба рассмотрена, статус: {resolution}"
            });

            return Result<string>.Ok("Жалоба рассмотрена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при рассмотрении жалобы {Id}", id);
            return Result<string>.Fail("Не удалось рассмотреть жалобу", ErrorType.InternalServerError);
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
