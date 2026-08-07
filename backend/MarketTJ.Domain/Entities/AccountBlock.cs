namespace MarketTJ.Domain.Entities;

// Технический/дисциплинарный бан аккаунта (Блок 2 + Блок 3, 2026-08-08, по
// явному запросу пользователя) — один и тот же механизм для 3+ отмен за 24ч
// (BlockType=Cancellations) И для спам-кликов (Блок 3, BlockType=RateLimit),
// одна и та же admin-страница "Заблокированные аккаунты" показывает оба типа.
// Активность бана НЕ хранится отдельным флагом — вычисляется на лету:
// BlockedUntil > now && UnblockedAt == null (тот же приём, что и с "занят"
// курьером в Item 5 этой сессии — не нужно ничего "восстанавливать" по
// истечении времени, фоновая задача не требуется).
public class AccountBlock
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = null!;
    public string BlockType { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public DateTime BlockedAt { get; set; }
    public DateTime BlockedUntil { get; set; }
    public DateTime? UnblockedAt { get; set; }
    public int? UnblockedByAdminId { get; set; }

    public User User { get; set; } = null!;
}
