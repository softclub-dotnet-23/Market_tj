using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AccountBlockDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAccountBlockService
{
    // Валидирует причину (минимум несколько слов), записывает отмену,
    // считает нарушения за 24ч у (userId, role) и при достижении порога
    // (3) создаёт бан — сама создаёт его только если ещё нет активного.
    // Data != null означает, что ИМЕННО этим вызовом создан новый бан —
    // вызывающий код (DeliveryService/OrderService) должен вернуть это
    // сообщение как часть ответа пользователю.
    Task<Result<string?>> RecordCancellationAsync(int userId, string role, int orderId, string reason);

    // Для проверки перед действием, которое должно быть запрещено во время
    // бана (взять новую доставку, принять новый заказ).
    Task<AccountBlock?> GetActiveBlockAsync(int userId);

    // Массовая версия для списков (напр. GetAvailableCouriersAsync) — какие
    // из перечисленных userId сейчас активно заблокированы.
    Task<HashSet<int>> GetActiveBlockedUserIdsAsync(IEnumerable<int> userIds);

    // Общее создание бана — переиспользуется Блоком 3 (rate-limit, RateLimitService)
    // с другим blockType и СВОЕЙ парой длительностей (5 мин / 30 мин), а не
    // только Блоком 2 (RecordCancellationAsync, 48ч / 7д). Если обе не заданы —
    // используются дефолты Блока 2 (для обратной совместимости вызовов без
    // явных длительностей). Эскалация (2-й+ параметр) применяется всегда
    // одинаково — "уже был бан этого типа раньше" → длительность из
    // escalatedDuration, иначе firstOffenseDuration.
    Task<AccountBlock> CreateBlockAsync(
        int userId, string role, string blockType, string reason,
        TimeSpan? firstOffenseDuration = null, TimeSpan? escalatedDuration = null);

    Task<Result<PagedResult<GetAccountBlockDto>>> GetAllAsync(PagedRequest request, bool? activeOnly);
    Task<Result<string>> UnblockAsync(int blockId);
}
