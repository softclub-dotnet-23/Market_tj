using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CommissionDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class CommissionService(
    ICommissionRepository commissionRepository,
    ICategoryRepository categoryRepository,
    ILogger<CommissionService> logger) : ICommissionService
{
    public async Task<Result<IEnumerable<GetCommissionDto>>> GetAllAsync()
    {
        try
        {
            var commissions = await commissionRepository.GetAllAsync();
            return Result<IEnumerable<GetCommissionDto>>.Ok(commissions.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка комиссий");
            return Result<IEnumerable<GetCommissionDto>>.Fail("Не удалось получить список комиссий", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetCommissionDto?>> GetByIdAsync(int id)
    {
        try
        {
            var commission = await commissionRepository.GetByIdAsync(id);
            if (commission is null)
                return Result<GetCommissionDto?>.Fail("Комиссия не найдена", ErrorType.NotFound);

            return Result<GetCommissionDto?>.Ok(ToGetDto(commission));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении комиссии {Id}", id);
            return Result<GetCommissionDto?>.Fail("Не удалось получить комиссию", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateCommissionDto dto)
    {
        try
        {
            var validation = CommissionValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            if (dto.CategoryId is not null)
            {
                var category = await categoryRepository.GetByIdAsync(dto.CategoryId.Value);
                if (category is null)
                    return Result<string>.Fail("Категория не найдена", ErrorType.NotFound);
            }

            var commission = new Commission
            {
                CategoryId = dto.CategoryId,
                Percentage = dto.Percentage,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                CreatedAt = DateTime.UtcNow
            };

            await commissionRepository.AddAsync(commission);
            return Result<string>.Ok("Комиссия создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании комиссии");
            return Result<string>.Fail("Не удалось создать комиссию", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateCommissionDto dto)
    {
        try
        {
            var validation = CommissionValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var commission = await commissionRepository.GetByIdAsync(id);
            if (commission is null)
                return Result<string>.Fail("Комиссия не найдена", ErrorType.NotFound);

            if (dto.CategoryId is not null)
            {
                var category = await categoryRepository.GetByIdAsync(dto.CategoryId.Value);
                if (category is null)
                    return Result<string>.Fail("Категория не найдена", ErrorType.NotFound);
            }

            commission.CategoryId = dto.CategoryId;
            commission.Percentage = dto.Percentage;
            commission.EffectiveFrom = dto.EffectiveFrom;
            commission.EffectiveTo = dto.EffectiveTo;

            await commissionRepository.UpdateAsync(commission);
            return Result<string>.Ok("Комиссия обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении комиссии {Id}", id);
            return Result<string>.Fail("Не удалось обновить комиссию", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var commission = await commissionRepository.GetByIdAsync(id);
            if (commission is null)
                return Result<string>.Fail("Комиссия не найдена", ErrorType.NotFound);

            await commissionRepository.DeleteAsync(commission);
            return Result<string>.Ok("Комиссия удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении комиссии {Id}", id);
            return Result<string>.Fail("Не удалось удалить комиссию", ErrorType.InternalServerError);
        }
    }

    private static GetCommissionDto ToGetDto(Commission commission) => new()
    {
        Id = commission.Id,
        CategoryId = commission.CategoryId,
        Percentage = commission.Percentage,
        EffectiveFrom = commission.EffectiveFrom,
        EffectiveTo = commission.EffectiveTo,
        CreatedAt = commission.CreatedAt
    };
}
