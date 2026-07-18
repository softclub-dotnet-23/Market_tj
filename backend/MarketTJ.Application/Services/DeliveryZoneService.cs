using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliveryZoneDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class DeliveryZoneService(
    IDeliveryZoneRepository deliveryZoneRepository,
    ILogger<DeliveryZoneService> logger) : IDeliveryZoneService
{
    public async Task<Result<IEnumerable<GetDeliveryZoneDto>>> GetAllAsync()
    {
        try
        {
            var zones = await deliveryZoneRepository.GetAllAsync();
            return Result<IEnumerable<GetDeliveryZoneDto>>.Ok(zones.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка зон доставки");
            return Result<IEnumerable<GetDeliveryZoneDto>>.Fail("Не удалось получить список зон доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetDeliveryZoneDto?>> GetByIdAsync(int id)
    {
        try
        {
            var zone = await deliveryZoneRepository.GetByIdAsync(id);
            if (zone is null)
                return Result<GetDeliveryZoneDto?>.Fail("Зона доставки не найдена", ErrorType.NotFound);

            return Result<GetDeliveryZoneDto?>.Ok(ToGetDto(zone));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении зоны доставки {Id}", id);
            return Result<GetDeliveryZoneDto?>.Fail("Не удалось получить зону доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateDeliveryZoneDto dto)
    {
        try
        {
            var validation = DeliveryZoneValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var all = await deliveryZoneRepository.GetAllAsync();
            if (all.Any(z => z.Region == dto.Region && z.District == dto.District))
                return Result<string>.Fail("Зона доставки для этого региона и района уже существует", ErrorType.Conflict);

            var zone = new DeliveryZone
            {
                Region = dto.Region,
                District = dto.District,
                BasePrice = dto.BasePrice,
                PricePerKm = dto.PricePerKm,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await deliveryZoneRepository.AddAsync(zone);
            return Result<string>.Ok("Зона доставки создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании зоны доставки");
            return Result<string>.Fail("Не удалось создать зону доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateDeliveryZoneDto dto)
    {
        try
        {
            var validation = DeliveryZoneValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var zone = await deliveryZoneRepository.GetByIdAsync(id);
            if (zone is null)
                return Result<string>.Fail("Зона доставки не найдена", ErrorType.NotFound);

            var all = await deliveryZoneRepository.GetAllAsync();
            if (all.Any(z => z.Id != id && z.Region == dto.Region && z.District == dto.District))
                return Result<string>.Fail("Зона доставки для этого региона и района уже существует", ErrorType.Conflict);

            zone.Region = dto.Region;
            zone.District = dto.District;
            zone.BasePrice = dto.BasePrice;
            zone.PricePerKm = dto.PricePerKm;
            zone.IsActive = dto.IsActive;
            zone.UpdatedAt = DateTime.UtcNow;

            await deliveryZoneRepository.UpdateAsync(zone);
            return Result<string>.Ok("Зона доставки обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении зоны доставки {Id}", id);
            return Result<string>.Fail("Не удалось обновить зону доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var zone = await deliveryZoneRepository.GetByIdAsync(id);
            if (zone is null)
                return Result<string>.Fail("Зона доставки не найдена", ErrorType.NotFound);

            await deliveryZoneRepository.DeleteAsync(zone);
            return Result<string>.Ok("Зона доставки удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении зоны доставки {Id}", id);
            return Result<string>.Fail("Не удалось удалить зону доставки", ErrorType.InternalServerError);
        }
    }

    private static GetDeliveryZoneDto ToGetDto(DeliveryZone zone) => new()
    {
        Id = zone.Id,
        Region = zone.Region,
        District = zone.District,
        BasePrice = zone.BasePrice,
        PricePerKm = zone.PricePerKm,
        IsActive = zone.IsActive,
        CreatedAt = zone.CreatedAt,
        UpdatedAt = zone.UpdatedAt
    };
}
