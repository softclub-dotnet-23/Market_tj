using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class ConversationRepository(AppDbContext context) : IConversationRepository
{
    public async Task<List<Conversation>> GetAllAsync()
        => await context.Conversations.ToListAsync();

    public async Task<Conversation?> GetByIdAsync(int id)
        => await context.Conversations.FindAsync(id);

    public async Task AddAsync(Conversation conversation)
    {
        await context.Conversations.AddAsync(conversation);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Conversation conversation)
    {
        context.Conversations.Update(conversation);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Conversation conversation)
    {
        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync();
    }
}
