using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    // Одна (любая) карта пользователя — используется там, где важен сам факт
    // "есть ли карта" и не важно, какая именно (например, публичная карта
    // фермера для оплаты, начисление фермеру — см. WalletService, берётся
    // самая старая карта из GetAllByUserIdAsync).
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<Wallet?> GetByIdAsync(int id);

    // Все карты пользователя (до 5) — для списка карт в личном кабинете и
    // для подсчёта лимита в WalletService.CreateAsync.
    Task<List<Wallet>> GetAllByUserIdAsync(int userId);

    // Возвращает false вместо исключения при гонке двух параллельных запросов
    // на создание — Version/Id генерируется БД, здесь ловим только реальные
    // ошибки записи (не unique-constraint — лимит в 5 карт проверяется на
    // уровне приложения в WalletService, не в БД, см. обоснование в
    // WalletConfiguration; TOCTOU-гонка при создании 6-й карты двумя
    // параллельными запросами теоретически возможна и не устраняется здесь).
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

    // Возврат в многокарточном мире должен попасть на ту же карту, с которой
    // было исходное списание, а не "любую карту покупателя" — источник
    // истины: строка Purchase-транзакции по этому заказу (см.
    // WalletService.RefundForOrderAsync). Null, если списания не было
    // (например, заказ был оплачен наличными).
    Task<WalletTransaction?> FindPurchaseTransactionForOrderAsync(int orderId);
}
