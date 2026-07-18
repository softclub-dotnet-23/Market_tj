using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class DeliveryService(
    IDeliveryRepository deliveryRepository,
    IOrderRepository orderRepository,
    ICourierProfileRepository courierProfileRepository,
    ILogger<DeliveryService> logger) : IDeliveryService
{
    public async Task<Result<IEnumerable<GetDeliveryDto>>> GetAllAsync()
    {
        try
        {
            var deliveries = await deliveryRepository.GetAllAsync();
            return Result<IEnumerable<GetDeliveryDto>>.Ok(deliveries.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка доставок");
            return Result<IEnumerable<GetDeliveryDto>>.Fail("Не удалось получить список доставок", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetDeliveryDto?>> GetByIdAsync(int id)
    {
        try
        {
            var delivery = await deliveryRepository.GetByIdAsync(id);
            if (delivery is null)
                return Result<GetDeliveryDto?>.Fail("Доставка не найдена", ErrorType.NotFound);

            return Result<GetDeliveryDto?>.Ok(ToGetDto(delivery));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении доставки {Id}", id);
            return Result<GetDeliveryDto?>.Fail("Не удалось получить доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateDeliveryDto dto)
    {
        try
        {
            var validation = DeliveryValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 8.12 ТЗ: один заказ имеет максимум одну активную доставку.
            var all = await deliveryRepository.GetAllAsync();
            if (all.Any(d => d.OrderId == dto.OrderId))
                return Result<string>.Fail("У этого заказа уже есть доставка", ErrorType.Conflict);

            if (dto.CourierId is not null)
            {
                var courier = await courierProfileRepository.GetByIdAsync(dto.CourierId.Value);
                if (courier is null)
                    return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

                // Раздел 10.5 ТЗ: нельзя назначить курьеру, у которого уже есть
                // активная (не завершённая/не отменённая) доставка одновременно.
                var hasActiveDelivery = all.Any(d => d.CourierId == dto.CourierId
                    && d.Status is not (DeliveryStatus.Delivered or DeliveryStatus.Cancelled));
                if (hasActiveDelivery)
                    return Result<string>.Fail("У курьера уже есть активная доставка — конфликтующее назначение запрещено", ErrorType.Conflict);
            }

            var delivery = new Delivery
            {
                OrderId = dto.OrderId,
                CourierId = dto.CourierId,
                PickupAddress = dto.PickupAddress,
                DeliveryAddress = dto.DeliveryAddress,
                DeliveryPrice = dto.DeliveryPrice,
                Status = dto.Status,
                AssignedAt = dto.CourierId is not null ? dto.AssignedAt ?? DateTime.UtcNow : null,
                PickedUpAt = dto.PickedUpAt,
                DeliveredAt = dto.DeliveredAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await deliveryRepository.AddAsync(delivery);
            return Result<string>.Ok("Доставка создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании доставки");
            return Result<string>.Fail("Не удалось создать доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateDeliveryDto dto)
    {
        try
        {
            var validation = DeliveryValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var delivery = await deliveryRepository.GetByIdAsync(id);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            var all = await deliveryRepository.GetAllAsync();
            if (all.Any(d => d.Id != id && d.OrderId == dto.OrderId))
                return Result<string>.Fail("У этого заказа уже есть доставка", ErrorType.Conflict);

            if (dto.CourierId is not null)
            {
                var courier = await courierProfileRepository.GetByIdAsync(dto.CourierId.Value);
                if (courier is null)
                    return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

                var hasActiveDelivery = all.Any(d => d.Id != id && d.CourierId == dto.CourierId
                    && d.Status is not (DeliveryStatus.Delivered or DeliveryStatus.Cancelled));
                if (hasActiveDelivery)
                    return Result<string>.Fail("У курьера уже есть активная доставка — конфликтующее назначение запрещено", ErrorType.Conflict);
            }

            delivery.OrderId = dto.OrderId;
            delivery.CourierId = dto.CourierId;
            delivery.PickupAddress = dto.PickupAddress;
            delivery.DeliveryAddress = dto.DeliveryAddress;
            delivery.DeliveryPrice = dto.DeliveryPrice;
            delivery.Status = dto.Status;
            delivery.AssignedAt = dto.AssignedAt ?? delivery.AssignedAt;
            delivery.PickedUpAt = dto.PickedUpAt;
            delivery.DeliveredAt = dto.DeliveredAt;
            delivery.UpdatedAt = DateTime.UtcNow;

            await deliveryRepository.UpdateAsync(delivery);
            return Result<string>.Ok("Доставка обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении доставки {Id}", id);
            return Result<string>.Fail("Не удалось обновить доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var delivery = await deliveryRepository.GetByIdAsync(id);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);

            await deliveryRepository.DeleteAsync(delivery);
            return Result<string>.Ok("Доставка удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении доставки {Id}", id);
            return Result<string>.Fail("Не удалось удалить доставку", ErrorType.InternalServerError);
        }
    }

    private static GetDeliveryDto ToGetDto(Delivery delivery) => new()
    {
        Id = delivery.Id,
        OrderId = delivery.OrderId,
        CourierId = delivery.CourierId,
        PickupAddress = delivery.PickupAddress,
        DeliveryAddress = delivery.DeliveryAddress,
        DeliveryPrice = delivery.DeliveryPrice,
        Status = delivery.Status,
        AssignedAt = delivery.AssignedAt,
        PickedUpAt = delivery.PickedUpAt,
        DeliveredAt = delivery.DeliveredAt,
        CreatedAt = delivery.CreatedAt,
        UpdatedAt = delivery.UpdatedAt
    };
}
