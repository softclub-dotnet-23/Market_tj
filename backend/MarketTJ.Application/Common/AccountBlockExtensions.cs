using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Common;

// Общее форматирование сообщения о блокировке (Блок 2 + Блок 3, 2026-08-08) —
// раньше было продублировано private-методом в DeliveryService и OrderService,
// вынесено сюда, т.к. теперь используется и в RateLimitService (Блок 3).
public static class AccountBlockExtensions
{
    public static string FormatBlockMessage(this AccountBlock block) =>
        $"Аккаунт временно заблокирован до {block.BlockedUntil:dd.MM.yyyy HH:mm} UTC. Причина: {block.Reason}.";
}
