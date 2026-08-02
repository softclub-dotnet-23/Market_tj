using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class WalletRepository(AppDbContext context) : IWalletRepository
{
    public async Task<Wallet?> GetByUserIdAsync(int userId)
        => await context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<Wallet?> GetByIdAsync(int id)
        => await context.Wallets.FindAsync(id);

    public async Task<bool> TryAddAsync(Wallet wallet)
    {
        await context.Wallets.AddAsync(wallet);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique index на UserId — гонка двух параллельных запросов на
            // создание карты одним пользователем.
            context.Entry(wallet).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryApplyTransactionAsync(Wallet wallet, WalletTransaction transaction)
    {
        // Инкремент Version — то самое изменение, которое EF Core сверяет с
        // исходным (прочитанным) значением в WHERE UPDATE-запроса; см.
        // WalletConfiguration.IsConcurrencyToken.
        wallet.Version++;
        context.Wallets.Update(wallet);
        await context.WalletTransactions.AddAsync(transaction);
        try
        {
            // Один SaveChangesAsync на оба изменения — EF Core оборачивает
            // их в одну транзакцию БД, поэтому баланс кошелька и строка
            // аудит-лога либо обе сохранятся, либо обе откатятся.
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            context.Entry(wallet).State = EntityState.Detached;
            context.Entry(transaction).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<List<WalletTransaction>> GetTransactionsAsync(int walletId)
        => await context.WalletTransactions.Where(t => t.WalletId == walletId).ToListAsync();
}
