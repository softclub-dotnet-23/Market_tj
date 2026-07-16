using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class SupportMessageRepository(AppDbContext context) : ISupportMessageRepository
{
    public async Task<List<SupportMessage>> GetAllAsync()
        => await context.SupportMessages.ToListAsync();

    public async Task<SupportMessage?> GetByIdAsync(int id)
        => await context.SupportMessages.FindAsync(id);

    public async Task AddAsync(SupportMessage supportMessage)
    {
        await context.SupportMessages.AddAsync(supportMessage);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SupportMessage supportMessage)
    {
        context.SupportMessages.Update(supportMessage);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(SupportMessage supportMessage)
    {
        context.SupportMessages.Remove(supportMessage);
        await context.SaveChangesAsync();
    }
}
