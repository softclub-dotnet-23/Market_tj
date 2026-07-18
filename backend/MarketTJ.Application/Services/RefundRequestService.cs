using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.RefundRequestDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class RefundRequestService(
    IRefundRequestRepository refundRequestRepository,
    IOrderRepository orderRepository,
    ICustomerProfileRepository customerProfileRepository,
    IUserRepository userRepository,
    ILogger<RefundRequestService> logger) : IRefundRequestService
{
    public async Task<Result<IEnumerable<GetRefundRequestDto>>> GetAllAsync()
    {
        try
        {
            var requests = await refundRequestRepository.GetAllAsync();
            return Result<IEnumerable<GetRefundRequestDto>>.Ok(requests.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка запросов на возврат");
            return Result<IEnumerable<GetRefundRequestDto>>.Fail("Не удалось получить список запросов на возврат", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetRefundRequestDto?>> GetByIdAsync(int id)
    {
        try
        {
            var request = await refundRequestRepository.GetByIdAsync(id);
            if (request is null)
                return Result<GetRefundRequestDto?>.Fail("Запрос на возврат не найден", ErrorType.NotFound);

            return Result<GetRefundRequestDto?>.Ok(ToGetDto(request));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении запроса на возврат {Id}", id);
            return Result<GetRefundRequestDto?>.Fail("Не удалось получить запрос на возврат", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateRefundRequestDto dto)
    {
        try
        {
            var validation = RefundRequestValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 8.21 ТЗ: RefundRequest.CustomerId — FK на User напрямую,
            // сверяем с UserId покупателя заказа (через профиль).
            var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
            if (customerProfile is null || customerProfile.UserId != dto.CustomerId)
                return Result<string>.Fail("CustomerId не соответствует покупателю заказа", ErrorType.Validation);

            if (dto.Amount > order.TotalAmount)
                return Result<string>.Fail("Amount не может превышать сумму заказа", ErrorType.Validation);

            if (dto.ProcessedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.ProcessedByAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("ProcessedByAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            // Бизнес-правило раздела 8.21: у одного Order не может быть двух
            // Pending RefundRequest одновременно.
            if (dto.Status == RefundStatus.Pending)
            {
                var all = await refundRequestRepository.GetAllAsync();
                if (all.Any(r => r.OrderId == dto.OrderId && r.Status == RefundStatus.Pending))
                    return Result<string>.Fail("У этого заказа уже есть запрос на возврат в статусе Pending", ErrorType.Conflict);
            }

            var request = new RefundRequest
            {
                OrderId = dto.OrderId,
                CustomerId = dto.CustomerId,
                Reason = dto.Reason,
                Amount = dto.Amount,
                Status = dto.Status,
                ProcessedAt = dto.ProcessedAt,
                ProcessedByAdminId = dto.ProcessedByAdminId,
                CreatedAt = DateTime.UtcNow
            };

            await refundRequestRepository.AddAsync(request);
            return Result<string>.Ok("Запрос на возврат создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании запроса на возврат");
            return Result<string>.Fail("Не удалось создать запрос на возврат", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateRefundRequestDto dto)
    {
        try
        {
            var validation = RefundRequestValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var request = await refundRequestRepository.GetByIdAsync(id);
            if (request is null)
                return Result<string>.Fail("Запрос на возврат не найден", ErrorType.NotFound);

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
            if (customerProfile is null || customerProfile.UserId != dto.CustomerId)
                return Result<string>.Fail("CustomerId не соответствует покупателю заказа", ErrorType.Validation);

            if (dto.Amount > order.TotalAmount)
                return Result<string>.Fail("Amount не может превышать сумму заказа", ErrorType.Validation);

            if (dto.ProcessedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.ProcessedByAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("ProcessedByAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            if (dto.Status == RefundStatus.Pending)
            {
                var all = await refundRequestRepository.GetAllAsync();
                if (all.Any(r => r.Id != id && r.OrderId == dto.OrderId && r.Status == RefundStatus.Pending))
                    return Result<string>.Fail("У этого заказа уже есть запрос на возврат в статусе Pending", ErrorType.Conflict);
            }

            request.OrderId = dto.OrderId;
            request.CustomerId = dto.CustomerId;
            request.Reason = dto.Reason;
            request.Amount = dto.Amount;
            request.Status = dto.Status;
            request.ProcessedAt = dto.ProcessedAt;
            request.ProcessedByAdminId = dto.ProcessedByAdminId;

            await refundRequestRepository.UpdateAsync(request);
            return Result<string>.Ok("Запрос на возврат обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении запроса на возврат {Id}", id);
            return Result<string>.Fail("Не удалось обновить запрос на возврат", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var request = await refundRequestRepository.GetByIdAsync(id);
            if (request is null)
                return Result<string>.Fail("Запрос на возврат не найден", ErrorType.NotFound);

            await refundRequestRepository.DeleteAsync(request);
            return Result<string>.Ok("Запрос на возврат удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении запроса на возврат {Id}", id);
            return Result<string>.Fail("Не удалось удалить запрос на возврат", ErrorType.InternalServerError);
        }
    }

    private static GetRefundRequestDto ToGetDto(RefundRequest request) => new()
    {
        Id = request.Id,
        OrderId = request.OrderId,
        CustomerId = request.CustomerId,
        Reason = request.Reason,
        Amount = request.Amount,
        Status = request.Status,
        CreatedAt = request.CreatedAt,
        ProcessedAt = request.ProcessedAt,
        ProcessedByAdminId = request.ProcessedByAdminId
    };
}
