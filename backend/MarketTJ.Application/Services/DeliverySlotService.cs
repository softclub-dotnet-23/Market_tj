using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliverySlotDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class DeliverySlotService(
    IDeliverySlotRepository deliverySlotRepository,
    IOrderRepository orderRepository,
    ILogger<DeliverySlotService> logger) : IDeliverySlotService
{
    public async Task<Result<IEnumerable<GetDeliverySlotDto>>> GetAllAsync()
    {
        try
        {
            var slots = await deliverySlotRepository.GetAllAsync();
            return Result<IEnumerable<GetDeliverySlotDto>>.Ok(slots.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка слотов доставки");
            return Result<IEnumerable<GetDeliverySlotDto>>.Fail("Не удалось получить список слотов доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetDeliverySlotDto?>> GetByIdAsync(int id)
    {
        try
        {
            var slot = await deliverySlotRepository.GetByIdAsync(id);
            if (slot is null)
                return Result<GetDeliverySlotDto?>.Fail("Слот доставки не найден", ErrorType.NotFound);

            return Result<GetDeliverySlotDto?>.Ok(ToGetDto(slot));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении слота доставки {Id}", id);
            return Result<GetDeliverySlotDto?>.Fail("Не удалось получить слот доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateDeliverySlotDto dto)
    {
        try
        {
            var validation = DeliverySlotValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 8.29 ТЗ: один слот на заказ.
            var all = await deliverySlotRepository.GetAllAsync();
            if (all.Any(s => s.OrderId == dto.OrderId))
                return Result<string>.Fail("У этого заказа уже есть слот доставки", ErrorType.Conflict);

            var slot = new DeliverySlot
            {
                OrderId = dto.OrderId,
                Date = dto.Date,
                TimeFrom = dto.TimeFrom,
                TimeTo = dto.TimeTo,
                CreatedAt = DateTime.UtcNow
            };

            await deliverySlotRepository.AddAsync(slot);
            return Result<string>.Ok("Слот доставки создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании слота доставки");
            return Result<string>.Fail("Не удалось создать слот доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateDeliverySlotDto dto)
    {
        try
        {
            var validation = DeliverySlotValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var slot = await deliverySlotRepository.GetByIdAsync(id);
            if (slot is null)
                return Result<string>.Fail("Слот доставки не найден", ErrorType.NotFound);

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            var all = await deliverySlotRepository.GetAllAsync();
            if (all.Any(s => s.Id != id && s.OrderId == dto.OrderId))
                return Result<string>.Fail("У этого заказа уже есть слот доставки", ErrorType.Conflict);

            slot.OrderId = dto.OrderId;
            slot.Date = dto.Date;
            slot.TimeFrom = dto.TimeFrom;
            slot.TimeTo = dto.TimeTo;

            await deliverySlotRepository.UpdateAsync(slot);
            return Result<string>.Ok("Слот доставки обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении слота доставки {Id}", id);
            return Result<string>.Fail("Не удалось обновить слот доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var slot = await deliverySlotRepository.GetByIdAsync(id);
            if (slot is null)
                return Result<string>.Fail("Слот доставки не найден", ErrorType.NotFound);

            await deliverySlotRepository.DeleteAsync(slot);
            return Result<string>.Ok("Слот доставки удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении слота доставки {Id}", id);
            return Result<string>.Fail("Не удалось удалить слот доставки", ErrorType.InternalServerError);
        }
    }

    private static GetDeliverySlotDto ToGetDto(DeliverySlot slot) => new()
    {
        Id = slot.Id,
        OrderId = slot.OrderId,
        Date = slot.Date,
        TimeFrom = slot.TimeFrom,
        TimeTo = slot.TimeTo,
        CreatedAt = slot.CreatedAt
    };
}
