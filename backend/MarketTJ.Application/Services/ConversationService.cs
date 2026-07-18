using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ConversationDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class ConversationService(
    IConversationRepository conversationRepository,
    IOrderRepository orderRepository,
    ICustomerProfileRepository customerProfileRepository,
    IFarmerProfileRepository farmerProfileRepository,
    ILogger<ConversationService> logger) : IConversationService
{
    public async Task<Result<IEnumerable<GetConversationDto>>> GetAllAsync()
    {
        try
        {
            var conversations = await conversationRepository.GetAllAsync();
            return Result<IEnumerable<GetConversationDto>>.Ok(conversations.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка чатов");
            return Result<IEnumerable<GetConversationDto>>.Fail("Не удалось получить список чатов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetConversationDto?>> GetByIdAsync(int id)
    {
        try
        {
            var conversation = await conversationRepository.GetByIdAsync(id);
            if (conversation is null)
                return Result<GetConversationDto?>.Fail("Чат не найден", ErrorType.NotFound);

            return Result<GetConversationDto?>.Ok(ToGetDto(conversation));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении чата {Id}", id);
            return Result<GetConversationDto?>.Fail("Не удалось получить чат", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateConversationDto dto)
    {
        try
        {
            var validation = ConversationValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 8.15 ТЗ: Conversation.CustomerId/FarmerId — это FK на User
            // (в отличие от Order, где связь идёт через профили), поэтому
            // сверяем их с UserId соответствующих профилей заказа.
            var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
            if (customerProfile is null || customerProfile.UserId != dto.CustomerId)
                return Result<string>.Fail("CustomerId не соответствует покупателю заказа", ErrorType.Validation);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
            if (farmerProfile is null || farmerProfile.UserId != dto.FarmerId)
                return Result<string>.Fail("FarmerId не соответствует фермеру заказа", ErrorType.Validation);

            // Раздел 8.15 ТЗ: один чат на заказ.
            var all = await conversationRepository.GetAllAsync();
            if (all.Any(c => c.OrderId == dto.OrderId))
                return Result<string>.Fail("Для этого заказа уже создан чат", ErrorType.Conflict);

            var conversation = new Conversation
            {
                OrderId = dto.OrderId,
                CustomerId = dto.CustomerId,
                FarmerId = dto.FarmerId,
                IsClosed = dto.IsClosed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await conversationRepository.AddAsync(conversation);
            return Result<string>.Ok("Чат создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании чата");
            return Result<string>.Fail("Не удалось создать чат", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateConversationDto dto)
    {
        try
        {
            var validation = ConversationValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var conversation = await conversationRepository.GetByIdAsync(id);
            if (conversation is null)
                return Result<string>.Fail("Чат не найден", ErrorType.NotFound);

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
            if (customerProfile is null || customerProfile.UserId != dto.CustomerId)
                return Result<string>.Fail("CustomerId не соответствует покупателю заказа", ErrorType.Validation);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
            if (farmerProfile is null || farmerProfile.UserId != dto.FarmerId)
                return Result<string>.Fail("FarmerId не соответствует фермеру заказа", ErrorType.Validation);

            var all = await conversationRepository.GetAllAsync();
            if (all.Any(c => c.Id != id && c.OrderId == dto.OrderId))
                return Result<string>.Fail("Для этого заказа уже создан чат", ErrorType.Conflict);

            conversation.OrderId = dto.OrderId;
            conversation.CustomerId = dto.CustomerId;
            conversation.FarmerId = dto.FarmerId;
            conversation.IsClosed = dto.IsClosed;
            conversation.UpdatedAt = DateTime.UtcNow;

            await conversationRepository.UpdateAsync(conversation);
            return Result<string>.Ok("Чат обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении чата {Id}", id);
            return Result<string>.Fail("Не удалось обновить чат", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var conversation = await conversationRepository.GetByIdAsync(id);
            if (conversation is null)
                return Result<string>.Fail("Чат не найден", ErrorType.NotFound);

            await conversationRepository.DeleteAsync(conversation);
            return Result<string>.Ok("Чат удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении чата {Id}", id);
            return Result<string>.Fail("Не удалось удалить чат", ErrorType.InternalServerError);
        }
    }

    private static GetConversationDto ToGetDto(Conversation conversation) => new()
    {
        Id = conversation.Id,
        OrderId = conversation.OrderId,
        CustomerId = conversation.CustomerId,
        FarmerId = conversation.FarmerId,
        IsClosed = conversation.IsClosed,
        CreatedAt = conversation.CreatedAt,
        UpdatedAt = conversation.UpdatedAt
    };
}
