using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class AiConversationLogRepository(AppDbContext context) : IAiConversationLogRepository
{
    public async Task<List<AiConversationLog>> GetAllAsync()
        => await context.AiConversationLogs.AsNoTracking().ToListAsync();

    public async Task AddAsync(AiConversationLog log)
    {
        await context.AiConversationLogs.AddAsync(log);
        await context.SaveChangesAsync();
    }
}
