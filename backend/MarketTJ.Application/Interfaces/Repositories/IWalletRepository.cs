using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<Wallet?> GetByIdAsync(int id);

    // Возвращает false вместо исключения, если у пользователя уже есть
    // кошелёк (unique index на UserId сработал под гонкой двух параллельных
    // запросов на создание карты) — вызывающий код (WalletService) не
    // должен знать о DbUpdateException, это деталь EF Core/Infrastructure.
    Task<bool> TryAddAsync(Wallet wallet);

    // Атомарно применяет новый баланс кошелька и вставляет строку
    // аудит-лога одним SaveChangesAsync (EF Core сам оборачивает несколько
    // изменений в одну транзакцию БД). Возвращает false вместо исключения
    // при конфликте оптимistic-конкуренции (xmin успел измениться между
    // чтением и записью — два параллельных списания с одного баланса),
    // чтобы WalletService мог перечитать свежий баланс и повторить, не имея
    // дела с EF-специфичными типами исключений через границу слоёв.
    Task<bool> TryApplyTransactionAsync(Wallet wallet, WalletTransaction transaction);

    Task<List<WalletTransaction>> GetTransactionsAsync(int walletId);
}
