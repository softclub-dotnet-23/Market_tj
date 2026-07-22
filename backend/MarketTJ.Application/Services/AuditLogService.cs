using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuditLogDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class AuditLogService(
    IAuditLogRepository auditLogRepository,
    IUserRepository userRepository,
    ILogger<AuditLogService> logger) : IAuditLogService
{
    public async Task<Result<IEnumerable<GetAuditLogDto>>> GetAllAsync()
    {
        try
        {
            var logs = await auditLogRepository.GetAllAsync();
            return Result<IEnumerable<GetAuditLogDto>>.Ok(logs.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка журналов действий");
            return Result<IEnumerable<GetAuditLogDto>>.Fail("Не удалось получить список журналов действий", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetAuditLogDto?>> GetByIdAsync(int id)
    {
        try
        {
            var log = await auditLogRepository.GetByIdAsync(id);
            if (log is null)
                return Result<GetAuditLogDto?>.Fail("Запись журнала не найдена", ErrorType.NotFound);

            return Result<GetAuditLogDto?>.Ok(ToGetDto(log));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении записи журнала {Id}", id);
            return Result<GetAuditLogDto?>.Fail("Не удалось получить запись журнала", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateAuditLogDto dto)
    {
        try
        {
            var validation = AuditLogValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var admin = await userRepository.GetByIdAsync(dto.AdminId);
            if (admin is null)
                return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

            if (admin.Role != UserRole.Admin)
                return Result<string>.Fail("AdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);

            var log = new AuditLog
            {
                AdminId = dto.AdminId,
                Action = dto.Action,
                EntityType = dto.EntityType,
                EntityId = dto.EntityId,
                Details = dto.Details,
                CreatedAt = DateTime.UtcNow
            };

            await auditLogRepository.AddAsync(log);
            return Result<string>.Ok("Запись журнала создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании записи журнала");
            return Result<string>.Fail("Не удалось создать запись журнала", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateAuditLogDto dto)
    {
        try
        {
            var validation = AuditLogValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var log = await auditLogRepository.GetByIdAsync(id);
            if (log is null)
                return Result<string>.Fail("Запись журнала не найдена", ErrorType.NotFound);

            var admin = await userRepository.GetByIdAsync(dto.AdminId);
            if (admin is null)
                return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

            if (admin.Role != UserRole.Admin)
                return Result<string>.Fail("AdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);

            log.AdminId = dto.AdminId;
            log.Action = dto.Action;
            log.EntityType = dto.EntityType;
            log.EntityId = dto.EntityId;
            log.Details = dto.Details;

            await auditLogRepository.UpdateAsync(log);
            return Result<string>.Ok("Запись журнала обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении записи журнала {Id}", id);
            return Result<string>.Fail("Не удалось обновить запись журнала", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var log = await auditLogRepository.GetByIdAsync(id);
            if (log is null)
                return Result<string>.Fail("Запись журнала не найдена", ErrorType.NotFound);

            await auditLogRepository.DeleteAsync(log);
            return Result<string>.Ok("Запись журнала удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении записи журнала {Id}", id);
            return Result<string>.Fail("Не удалось удалить запись журнала", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<PagedResult<GetAuditLogDto>>> GetPagedAsync(PagedRequest request, DateTime? from, DateTime? to, int? adminId, string? action)
    {
        try
        {
            var all = await auditLogRepository.GetAllAsync();

            IEnumerable<AuditLog> filtered = all;
            if (from is not null)
                filtered = filtered.Where(l => l.CreatedAt >= from);
            if (to is not null)
                filtered = filtered.Where(l => l.CreatedAt <= to);
            if (adminId is not null)
                filtered = filtered.Where(l => l.AdminId == adminId);
            if (!string.IsNullOrWhiteSpace(action))
                filtered = filtered.Where(l => l.Action.Contains(action, StringComparison.OrdinalIgnoreCase));

            // Журнал действий по умолчанию — самые свежие записи первыми.
            filtered = request.SortDescending || request.SortBy is null
                ? filtered.OrderByDescending(l => l.CreatedAt)
                : filtered.OrderBy(l => l.CreatedAt);

            var materialized = filtered.ToList();
            var page = materialized
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ToGetDto)
                .ToList();

            return Result<PagedResult<GetAuditLogDto>>.Ok(
                PagedResult<GetAuditLogDto>.Ok(page, materialized.Count, request.PageNumber, request.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении журнала действий (paged)");
            return Result<PagedResult<GetAuditLogDto>>.Fail("Не удалось получить журнал действий", ErrorType.InternalServerError);
        }
    }

    private static GetAuditLogDto ToGetDto(AuditLog log) => new()
    {
        Id = log.Id,
        AdminId = log.AdminId,
        Action = log.Action,
        EntityType = log.EntityType,
        EntityId = log.EntityId,
        Details = log.Details,
        CreatedAt = log.CreatedAt
    };
}
