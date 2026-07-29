using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ConversationDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class ConversationService(
    IConversationRepository conversationRepository,
    IOrderRepository orderRepository,
    ICustomerProfileRepository customerProfileRepository,
    IFarmerProfileRepository farmerProfileRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    ILogger<ConversationService> logger) : IConversationService
{
    public async Task<Result<IEnumerable<GetConversationDto>>> GetAllAsync()
    {
        try
        {
            var conversations = await conversationRepository.GetAllAsync();

            // Audit 2026-07-28, находка 2.2 (IDOR): CustomerId/FarmerId — User.Id
            // напрямую (см. комментарий у сущности) — фильтруем по участникам.
            if (!currentUser.IsAdmin())
                conversations = conversations.Where(c => c.CustomerId == currentUser.UserId || c.FarmerId == currentUser.UserId).ToList();

            var customerNames = await ResolveCustomerFullNamesAsync(conversations.Select(c => c.CustomerId));
            return Result<IEnumerable<GetConversationDto>>.Ok(conversations.Select(c => ToGetDto(c, customerNames)));
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

            if (!currentUser.IsAdmin() && conversation.CustomerId != currentUser.UserId && conversation.FarmerId != currentUser.UserId)
                return Result<GetConversationDto?>.Fail("Нет доступа к этому чату", ErrorType.Forbidden);

            var customerNames = await ResolveCustomerFullNamesAsync([conversation.CustomerId]);
            return Result<GetConversationDto?>.Ok(ToGetDto(conversation, customerNames));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении чата {Id}", id);
            return Result<GetConversationDto?>.Fail("Не удалось получить чат", ErrorType.InternalServerError);
        }
    }

    // Conversation.CustomerId — это User.Id напрямую (в отличие от
    // Order.CustomerId, который указывает на CustomerProfile), поэтому здесь
    // достаточно прямой пакетной выборки без промежуточного джойна через
    // профиль — см. аналогичный, но двухшаговый OrderService.ResolveCustomerContactsAsync.
    private async Task<Dictionary<int, string?>> ResolveCustomerFullNamesAsync(IEnumerable<int> customerUserIds)
    {
        var neededIds = customerUserIds.Distinct().ToHashSet();
        var users = await userRepository.GetAllAsync();
        return users.Where(u => neededIds.Contains(u.Id)).ToDictionary(u => u.Id, u => (string?)u.FullName);
    }

    public async Task<Result<string>> CreateAsync(CreateConversationDto dto)
    {
        try
        {
            var validation = ConversationValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var participantsError = dto.OrderId.HasValue
                ? await ValidateOrderParticipantsAsync(dto.OrderId.Value, dto.CustomerId, dto.FarmerId)
                : await ValidatePreOrderParticipantsAsync(dto.CustomerId, dto.FarmerId);
            if (participantsError is not null)
                return participantsError;

            var all = await conversationRepository.GetAllAsync();
            var duplicateError = ValidatePairFree(all, dto.CustomerId, dto.FarmerId, excludeId: null);
            if (duplicateError is not null)
                return duplicateError;

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

            if (!currentUser.IsAdmin() && conversation.CustomerId != currentUser.UserId && conversation.FarmerId != currentUser.UserId)
                return Result<string>.Fail("Нет доступа к этому чату", ErrorType.Forbidden);

            var participantsError = dto.OrderId.HasValue
                ? await ValidateOrderParticipantsAsync(dto.OrderId.Value, dto.CustomerId, dto.FarmerId)
                : await ValidatePreOrderParticipantsAsync(dto.CustomerId, dto.FarmerId);
            if (participantsError is not null)
                return participantsError;

            var all = await conversationRepository.GetAllAsync();
            var duplicateError = ValidatePairFree(all, dto.CustomerId, dto.FarmerId, excludeId: id);
            if (duplicateError is not null)
                return duplicateError;

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

    // Раздел 8.15 ТЗ: Conversation.CustomerId/FarmerId — это FK на User (в
    // отличие от Order, где связь идёт через профили), поэтому сверяем их с
    // UserId соответствующих профилей заказа.
    private async Task<Result<string>?> ValidateOrderParticipantsAsync(int orderId, int customerUserId, int farmerUserId)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        if (order is null)
            return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

        var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
        if (customerProfile is null || customerProfile.UserId != customerUserId)
            return Result<string>.Fail("CustomerId не соответствует покупателю заказа", ErrorType.Validation);

        var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
        if (farmerProfile is null || farmerProfile.UserId != farmerUserId)
            return Result<string>.Fail("FarmerId не соответствует фермеру заказа", ErrorType.Validation);

        return null;
    }

    // Чат до заказа (вопрос фермеру про товар) — заказа ещё нет, поэтому
    // участников ищем напрямую по User.Id через профили. Фермер должен быть
    // подтверждён — до подтверждения его товары и так не видны в каталоге.
    private async Task<Result<string>?> ValidatePreOrderParticipantsAsync(int customerUserId, int farmerUserId)
    {
        var customerProfile = await customerProfileRepository.GetByUserIdAsync(customerUserId);
        if (customerProfile is null)
            return Result<string>.Fail("CustomerId не соответствует покупателю", ErrorType.Validation);

        var farmerProfile = await farmerProfileRepository.GetByUserIdAsync(farmerUserId);
        if (farmerProfile is null || farmerProfile.VerificationStatus != FarmerVerificationStatus.Verified)
            return Result<string>.Fail("FarmerId не соответствует подтверждённому фермеру", ErrorType.Validation);

        return null;
    }

    // Один чат на пару покупатель-фермер, независимо от заказов (раньше было
    // "один чат на заказ" + отдельный "до заказа" — так у одного и того же
    // клиента с одним фермером легко копились дублирующие переписки: общий
    // вопрос про товар отдельно от переписки по каждому новому заказу.
    // Пользователь явно попросила объединить в единый непрерывный тред, как в
    // обычном мессенджере — OrderId остаётся на Conversation только как
    // информация о том, с чего начался разговор, но больше не участвует в
    // проверке уникальности).
    private static Result<string>? ValidatePairFree(IEnumerable<Conversation> all, int customerUserId, int farmerUserId, int? excludeId)
        => all.Any(c => c.Id != excludeId && c.CustomerId == customerUserId && c.FarmerId == farmerUserId)
            ? Result<string>.Fail("Чат с этим пользователем уже есть", ErrorType.Conflict)
            : null;

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var conversation = await conversationRepository.GetByIdAsync(id);
            if (conversation is null)
                return Result<string>.Fail("Чат не найден", ErrorType.NotFound);

            if (!currentUser.IsAdmin() && conversation.CustomerId != currentUser.UserId && conversation.FarmerId != currentUser.UserId)
                return Result<string>.Fail("Нет доступа к этому чату", ErrorType.Forbidden);

            await conversationRepository.DeleteAsync(conversation);
            return Result<string>.Ok("Чат удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении чата {Id}", id);
            return Result<string>.Fail("Не удалось удалить чат", ErrorType.InternalServerError);
        }
    }

    private static GetConversationDto ToGetDto(Conversation conversation, IReadOnlyDictionary<int, string?> customerNames)
    {
        customerNames.TryGetValue(conversation.CustomerId, out var customerFullName);
        return new()
        {
            Id = conversation.Id,
            OrderId = conversation.OrderId,
            CustomerId = conversation.CustomerId,
            FarmerId = conversation.FarmerId,
            CustomerFullName = customerFullName,
            IsClosed = conversation.IsClosed,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }
}
