using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class AccountBlockRepository(AppDbContext context) : IAccountBlockRepository
{
    public async Task<List<AccountBlock>> GetAllAsync()
        => await context.AccountBlocks.AsNoTracking().ToListAsync();

    public async Task<AccountBlock?> GetByIdAsync(int id)
        => await context.AccountBlocks.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<AccountBlock?> GetActiveAsync(int userId, DateTime now)
        => await context.AccountBlocks
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.UnblockedAt == null && x.BlockedUntil > now)
            .OrderByDescending(x => x.BlockedAt)
            .FirstOrDefaultAsync();

    public async Task<List<int>> GetActiveUserIdsAsync(IEnumerable<int> userIds, DateTime now)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.AccountBlocks
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId) && x.UnblockedAt == null && x.BlockedUntil > now)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();
    }

    // "Прежние" баны того же типа — для эскалации длительности (Блок 2.5):
    // считаются все когда-либо созданные баны этого типа, активен он сейчас
    // или уже истёк/снят вручную — сам факт, что бан уже случался раньше,
    // и есть признак повторного нарушения.
    public async Task<int> CountPriorAsync(int userId, string blockType)
        => await context.AccountBlocks
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.BlockType == blockType)
            .CountAsync();

    public async Task AddAsync(AccountBlock block)
    {
        await context.AccountBlocks.AddAsync(block);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AccountBlock block)
    {
        context.AccountBlocks.Update(block);
        await context.SaveChangesAsync();
    }
}
