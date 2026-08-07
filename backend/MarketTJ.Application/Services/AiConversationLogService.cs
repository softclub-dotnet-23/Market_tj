using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class AiConversationLogService(
    IAiConversationLogRepository repository,
    IUserRepository userRepository,
    ILogger<AiConversationLogService> logger) : IAiConversationLogService
{
    // Логирование никогда не должно ронять реальный ответ ассистента —
    // вызывающая сторона (AiAssistantService) не должна упасть, если сама
    // запись в журнал не удалась (например, БД временно недоступна).
    public async Task LogAsync(int? userId, string role, string question, string response, string intent, bool wasError)
    {
        try
        {
            await repository.AddAsync(new AiConversationLog
            {
                UserId = userId,
                Role = role,
                Question = question,
                Response = response,
                Intent = intent,
                CreatedAt = DateTime.UtcNow,
                WasError = wasError
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось записать диалог AI-ассистента в журнал");
        }
    }

    public async Task<Result<PagedResult<GetAiConversationLogDto>>> GetPagedAsync(PagedRequest request, bool? wasError)
    {
        try
        {
            var all = await repository.GetAllAsync();

            IEnumerable<AiConversationLog> filtered = all;
            if (wasError is not null)
                filtered = filtered.Where(l => l.WasError == wasError);

            filtered = filtered.OrderByDescending(l => l.CreatedAt);

            var materialized = filtered.ToList();

            var userIds = materialized.Where(l => l.UserId is not null).Select(l => l.UserId!.Value).Distinct().ToList();
            var users = userIds.Count > 0 ? await userRepository.GetAllAsync() : [];
            var userNamesById = users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.FullName);

            var page = materialized
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(l => ToGetDto(l, userNamesById))
                .ToList();

            return Result<PagedResult<GetAiConversationLogDto>>.Ok(
                PagedResult<GetAiConversationLogDto>.Ok(page, materialized.Count, request.PageNumber, request.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении журнала диалогов AI-ассистента");
            return Result<PagedResult<GetAiConversationLogDto>>.Fail("Не удалось получить журнал диалогов", ErrorType.InternalServerError);
        }
    }

    private static GetAiConversationLogDto ToGetDto(AiConversationLog log, Dictionary<int, string> userNamesById) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        UserFullName = log.UserId is not null ? userNamesById.GetValueOrDefault(log.UserId.Value) : null,
        Role = log.Role,
        Question = log.Question,
        Response = log.Response,
        Intent = log.Intent,
        CreatedAt = log.CreatedAt,
        WasError = log.WasError
    };
}
