using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.PaymentDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class PaymentService(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<Result<IEnumerable<GetPaymentDto>>> GetAllAsync()
    {
        try
        {
            var payments = await paymentRepository.GetAllAsync();
            return Result<IEnumerable<GetPaymentDto>>.Ok(payments.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка платежей");
            return Result<IEnumerable<GetPaymentDto>>.Fail("Не удалось получить список платежей", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetPaymentDto?>> GetByIdAsync(int id)
    {
        try
        {
            var payment = await paymentRepository.GetByIdAsync(id);
            if (payment is null)
                return Result<GetPaymentDto?>.Fail("Платёж не найден", ErrorType.NotFound);

            return Result<GetPaymentDto?>.Ok(ToGetDto(payment));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении платежа {Id}", id);
            return Result<GetPaymentDto?>.Fail("Не удалось получить платёж", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreatePaymentDto dto)
    {
        try
        {
            var validation = PaymentValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            var payment = new Payment
            {
                OrderId = dto.OrderId,
                Amount = dto.Amount,
                Method = dto.Method,
                Status = dto.Status,
                PaidAt = dto.PaidAt,
                TransactionReference = dto.TransactionReference,
                CreatedAt = DateTime.UtcNow
            };

            await paymentRepository.AddAsync(payment);
            return Result<string>.Ok("Платёж создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании платежа");
            return Result<string>.Fail("Не удалось создать платёж", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdatePaymentDto dto)
    {
        try
        {
            var validation = PaymentValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var payment = await paymentRepository.GetByIdAsync(id);
            if (payment is null)
                return Result<string>.Fail("Платёж не найден", ErrorType.NotFound);

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            payment.OrderId = dto.OrderId;
            payment.Amount = dto.Amount;
            payment.Method = dto.Method;
            payment.Status = dto.Status;
            payment.PaidAt = dto.PaidAt;
            payment.TransactionReference = dto.TransactionReference;

            await paymentRepository.UpdateAsync(payment);
            return Result<string>.Ok("Платёж обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении платежа {Id}", id);
            return Result<string>.Fail("Не удалось обновить платёж", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var payment = await paymentRepository.GetByIdAsync(id);
            if (payment is null)
                return Result<string>.Fail("Платёж не найден", ErrorType.NotFound);

            await paymentRepository.DeleteAsync(payment);
            return Result<string>.Ok("Платёж удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении платежа {Id}", id);
            return Result<string>.Fail("Не удалось удалить платёж", ErrorType.InternalServerError);
        }
    }

    private static GetPaymentDto ToGetDto(Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        Amount = payment.Amount,
        Method = payment.Method,
        Status = payment.Status,
        PaidAt = payment.PaidAt,
        TransactionReference = payment.TransactionReference,
        CreatedAt = payment.CreatedAt
    };
}
