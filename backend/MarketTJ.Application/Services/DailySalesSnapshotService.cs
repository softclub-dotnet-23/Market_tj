using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DailySalesSnapshotDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class DailySalesSnapshotService(
    IDailySalesSnapshotRepository dailySalesSnapshotRepository,
    ILogger<DailySalesSnapshotService> logger) : IDailySalesSnapshotService
{
    public async Task<Result<IEnumerable<GetDailySalesSnapshotDto>>> GetAllAsync()
    {
        try
        {
            var snapshots = await dailySalesSnapshotRepository.GetAllAsync();
            return Result<IEnumerable<GetDailySalesSnapshotDto>>.Ok(snapshots.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка снимков продаж");
            return Result<IEnumerable<GetDailySalesSnapshotDto>>.Fail("Не удалось получить список снимков продаж", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetDailySalesSnapshotDto?>> GetByIdAsync(int id)
    {
        try
        {
            var snapshot = await dailySalesSnapshotRepository.GetByIdAsync(id);
            if (snapshot is null)
                return Result<GetDailySalesSnapshotDto?>.Fail("Снимок продаж не найден", ErrorType.NotFound);

            return Result<GetDailySalesSnapshotDto?>.Ok(ToGetDto(snapshot));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении снимка продаж {Id}", id);
            return Result<GetDailySalesSnapshotDto?>.Fail("Не удалось получить снимок продаж", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateDailySalesSnapshotDto dto)
    {
        try
        {
            var validation = DailySalesSnapshotValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            // Раздел 8.30 ТЗ: уникально per день.
            var all = await dailySalesSnapshotRepository.GetAllAsync();
            if (all.Any(s => s.Date.Date == dto.Date.Date))
                return Result<string>.Fail("Снимок продаж за эту дату уже существует", ErrorType.Conflict);

            var snapshot = new DailySalesSnapshot
            {
                Date = dto.Date,
                TotalOrders = dto.TotalOrders,
                TotalRevenue = dto.TotalRevenue,
                TotalCommission = dto.TotalCommission,
                NewFarmers = dto.NewFarmers,
                NewCustomers = dto.NewCustomers,
                CompletedDeliveries = dto.CompletedDeliveries,
                CreatedAt = DateTime.UtcNow
            };

            await dailySalesSnapshotRepository.AddAsync(snapshot);
            return Result<string>.Ok("Снимок продаж создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании снимка продаж");
            return Result<string>.Fail("Не удалось создать снимок продаж", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateDailySalesSnapshotDto dto)
    {
        try
        {
            var validation = DailySalesSnapshotValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var snapshot = await dailySalesSnapshotRepository.GetByIdAsync(id);
            if (snapshot is null)
                return Result<string>.Fail("Снимок продаж не найден", ErrorType.NotFound);

            var all = await dailySalesSnapshotRepository.GetAllAsync();
            if (all.Any(s => s.Id != id && s.Date.Date == dto.Date.Date))
                return Result<string>.Fail("Снимок продаж за эту дату уже существует", ErrorType.Conflict);

            snapshot.Date = dto.Date;
            snapshot.TotalOrders = dto.TotalOrders;
            snapshot.TotalRevenue = dto.TotalRevenue;
            snapshot.TotalCommission = dto.TotalCommission;
            snapshot.NewFarmers = dto.NewFarmers;
            snapshot.NewCustomers = dto.NewCustomers;
            snapshot.CompletedDeliveries = dto.CompletedDeliveries;

            await dailySalesSnapshotRepository.UpdateAsync(snapshot);
            return Result<string>.Ok("Снимок продаж обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении снимка продаж {Id}", id);
            return Result<string>.Fail("Не удалось обновить снимок продаж", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var snapshot = await dailySalesSnapshotRepository.GetByIdAsync(id);
            if (snapshot is null)
                return Result<string>.Fail("Снимок продаж не найден", ErrorType.NotFound);

            await dailySalesSnapshotRepository.DeleteAsync(snapshot);
            return Result<string>.Ok("Снимок продаж удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении снимка продаж {Id}", id);
            return Result<string>.Fail("Не удалось удалить снимок продаж", ErrorType.InternalServerError);
        }
    }

    private static GetDailySalesSnapshotDto ToGetDto(DailySalesSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Date = snapshot.Date,
        TotalOrders = snapshot.TotalOrders,
        TotalRevenue = snapshot.TotalRevenue,
        TotalCommission = snapshot.TotalCommission,
        NewFarmers = snapshot.NewFarmers,
        NewCustomers = snapshot.NewCustomers,
        CompletedDeliveries = snapshot.CompletedDeliveries,
        CreatedAt = snapshot.CreatedAt
    };
}
