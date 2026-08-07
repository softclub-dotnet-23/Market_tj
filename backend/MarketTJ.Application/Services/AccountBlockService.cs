using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AccountBlockDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Блок 2 + Блок 3 (2026-08-08, по явному запросу пользователя) — единый
// механизм банов: 3+ отмены заказа за 24ч (курьер/фермер) ИЛИ спам-клики по
// одному эндпоинту (см. RateLimitMiddleware) создают запись AccountBlock
// одним и тем же CreateBlockAsync, отображаются на одной admin-странице.
public class AccountBlockService(
    IAccountBlockRepository blockRepository,
    IOrderCancellationRepository cancellationRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    ILogger<AccountBlockService> logger) : IAccountBlockService
{
    private const int MaxCancellationsPerWindow = 3;
    private static readonly TimeSpan CancellationWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan FirstBlockDuration = TimeSpan.FromHours(48);
    private static readonly TimeSpan EscalatedBlockDuration = TimeSpan.FromDays(7);
    private const int MinReasonWords = 3;
    private const int MinReasonLength = 10;

    public const string CancellationsBlockType = "Cancellations";

    public async Task<Result<string?>> RecordCancellationAsync(int userId, string role, int orderId, string reason)
    {
        try
        {
            var validationError = ValidateReason(reason);
            if (validationError is not null)
                return Result<string?>.Fail(validationError, ErrorType.Validation);

            await cancellationRepository.AddAsync(new OrderCancellation
            {
                UserId = userId,
                Role = role,
                OrderId = orderId,
                Reason = reason.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            var since = DateTime.UtcNow - CancellationWindow;
            var count = await cancellationRepository.CountSinceAsync(userId, role, since);

            if (count >= MaxCancellationsPerWindow)
            {
                var alreadyBlocked = await blockRepository.GetActiveAsync(userId, DateTime.UtcNow);
                if (alreadyBlocked is null)
                {
                    var roleLabel = role == "Courier" ? "курьером" : "фермером";
                    var block = await CreateBlockAsync(
                        userId, role, CancellationsBlockType,
                        $"{count} отмены заказов {roleLabel} за последние 24 часа");
                    return Result<string?>.Ok(BuildBlockNotice(block));
                }
            }

            return Result<string?>.Ok(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при записи отмены заказа пользователем {UserId}", userId);
            return Result<string?>.Fail("Не удалось обработать отмену заказа", ErrorType.InternalServerError);
        }
    }

    public async Task<AccountBlock?> GetActiveBlockAsync(int userId)
        => await blockRepository.GetActiveAsync(userId, DateTime.UtcNow);

    public async Task<HashSet<int>> GetActiveBlockedUserIdsAsync(IEnumerable<int> userIds)
        => [.. await blockRepository.GetActiveUserIdsAsync(userIds, DateTime.UtcNow)];

    public async Task<AccountBlock> CreateBlockAsync(int userId, string role, string blockType, string reason, TimeSpan? overrideDuration = null)
    {
        TimeSpan duration;
        if (overrideDuration is not null)
        {
            duration = overrideDuration.Value;
        }
        else
        {
            // Эскалация (2026-08-08, Блок 2.5): если у пользователя УЖЕ был
            // бан этого же типа раньше (истёкший или снятый вручную — сам
            // факт повтора нарушения важен, а не текущий статус того бана) —
            // следующий бан длиннее.
            var priorCount = await blockRepository.CountPriorAsync(userId, blockType);
            duration = priorCount > 0 ? EscalatedBlockDuration : FirstBlockDuration;
        }

        var block = new AccountBlock
        {
            UserId = userId,
            Role = role,
            BlockType = blockType,
            Reason = reason,
            BlockedAt = DateTime.UtcNow,
            BlockedUntil = DateTime.UtcNow.Add(duration)
        };
        await blockRepository.AddAsync(block);
        logger.LogWarning("Аккаунт {UserId} ({Role}) заблокирован до {Until} ({BlockType}): {Reason}", userId, role, block.BlockedUntil, blockType, reason);
        return block;
    }

    public async Task<Result<PagedResult<GetAccountBlockDto>>> GetAllAsync(PagedRequest request, bool? activeOnly)
    {
        try
        {
            var all = await blockRepository.GetAllAsync();
            var now = DateTime.UtcNow;

            IEnumerable<AccountBlock> filtered = all;
            if (activeOnly == true)
                filtered = filtered.Where(b => b.UnblockedAt == null && b.BlockedUntil > now);

            filtered = filtered.OrderByDescending(b => b.BlockedAt);
            var materialized = filtered.ToList();

            var userIds = materialized.Select(b => b.UserId).Distinct().ToList();
            var users = userIds.Count > 0 ? await userRepository.GetAllAsync() : [];
            var namesById = users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.FullName);

            var page = materialized
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => ToGetDto(b, namesById, now))
                .ToList();

            return Result<PagedResult<GetAccountBlockDto>>.Ok(
                PagedResult<GetAccountBlockDto>.Ok(page, materialized.Count, request.PageNumber, request.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка заблокированных аккаунтов");
            return Result<PagedResult<GetAccountBlockDto>>.Fail("Не удалось получить список заблокированных аккаунтов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UnblockAsync(int blockId)
    {
        try
        {
            if (!currentUser.IsAdmin() || currentUser.UserId is null)
                return Result<string>.Fail("Разблокировать аккаунт может только администратор", ErrorType.Forbidden);

            var block = await blockRepository.GetByIdAsync(blockId);
            if (block is null)
                return Result<string>.Fail("Запись о блокировке не найдена", ErrorType.NotFound);

            if (block.UnblockedAt is not null)
                return Result<string>.Fail("Аккаунт уже разблокирован", ErrorType.Conflict);

            block.UnblockedAt = DateTime.UtcNow;
            block.UnblockedByAdminId = currentUser.UserId.Value;
            await blockRepository.UpdateAsync(block);

            return Result<string>.Ok("Аккаунт разблокирован");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при разблокировке аккаунта {BlockId}", blockId);
            return Result<string>.Fail("Не удалось разблокировать аккаунт", ErrorType.InternalServerError);
        }
    }

    private static string? ValidateReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return "Укажите причину отмены";

        var trimmed = reason.Trim();
        if (trimmed.Length < MinReasonLength)
            return $"Причина отмены слишком короткая (минимум {MinReasonLength} символов)";

        var wordCount = trimmed.Split((char[]?)null!, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < MinReasonWords)
            return "Опишите причину отмены подробнее — недостаточно одного слова";

        return null;
    }

    private static string BuildBlockNotice(AccountBlock block) =>
        $"Аккаунт заблокирован до {block.BlockedUntil:dd.MM.yyyy HH:mm} UTC. Причина: {block.Reason}.";

    private static GetAccountBlockDto ToGetDto(AccountBlock block, Dictionary<int, string> namesById, DateTime now) => new()
    {
        Id = block.Id,
        UserId = block.UserId,
        UserFullName = namesById.GetValueOrDefault(block.UserId),
        Role = block.Role,
        BlockType = block.BlockType,
        Reason = block.Reason,
        BlockedAt = block.BlockedAt,
        BlockedUntil = block.BlockedUntil,
        UnblockedAt = block.UnblockedAt,
        IsActive = block.UnblockedAt == null && block.BlockedUntil > now
    };
}
