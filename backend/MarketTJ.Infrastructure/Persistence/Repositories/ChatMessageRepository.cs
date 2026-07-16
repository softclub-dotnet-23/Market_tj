using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class ChatMessageRepository(AppDbContext context) : IChatMessageRepository
{
    public async Task<List<ChatMessage>> GetAllAsync()
        => await context.ChatMessages.ToListAsync();

    public async Task<ChatMessage?> GetByIdAsync(int id)
        => await context.ChatMessages.FindAsync(id);

    public async Task AddAsync(ChatMessage chatMessage)
    {
        await context.ChatMessages.AddAsync(chatMessage);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ChatMessage chatMessage)
    {
        context.ChatMessages.Update(chatMessage);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ChatMessage chatMessage)
    {
        context.ChatMessages.Remove(chatMessage);
        await context.SaveChangesAsync();
    }
}
