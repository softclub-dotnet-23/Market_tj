using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IAccountBlockRepository
{
    Task<List<AccountBlock>> GetAllAsync();
    Task<AccountBlock?> GetByIdAsync(int id);
    Task<AccountBlock?> GetActiveAsync(int userId, DateTime now);

    // Массовая проверка (Блок 2, GetAvailableCouriersAsync) — исключить
    // заблокированных курьеров из списка "доступных" одним запросом вместо
    // N обращений GetActiveAsync на каждого кандидата.
    Task<List<int>> GetActiveUserIdsAsync(IEnumerable<int> userIds, DateTime now);
    Task<int> CountPriorAsync(int userId, string blockType);
    Task AddAsync(AccountBlock block);
    Task UpdateAsync(AccountBlock block);
}
