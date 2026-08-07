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

    // Общее создание бана — переиспользуется Блоком 3 (rate-limit) с другим
    // blockType, а не только Блоком 2 (RecordCancellationAsync).
    Task<AccountBlock> CreateBlockAsync(int userId, string role, string blockType, string reason, TimeSpan? overrideDuration = null);

    Task<Result<PagedResult<GetAccountBlockDto>>> GetAllAsync(PagedRequest request, bool? activeOnly);
    Task<Result<string>> UnblockAsync(int blockId);
}
