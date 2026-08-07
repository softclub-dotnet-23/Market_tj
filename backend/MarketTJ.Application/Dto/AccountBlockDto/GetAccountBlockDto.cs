namespace MarketTJ.Application.Dto.AccountBlockDto;

public class GetAccountBlockDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserFullName { get; set; }
    public string Role { get; set; } = null!;
    public string BlockType { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public DateTime BlockedAt { get; set; }
    public DateTime BlockedUntil { get; set; }
    public DateTime? UnblockedAt { get; set; }
    public bool IsActive { get; set; }
}
