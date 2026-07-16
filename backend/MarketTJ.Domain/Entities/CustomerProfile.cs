using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class CustomerProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public CustomerType CustomerType { get; set; }
    public string? DefaultAddress { get; set; }
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // User 1 — 1 CustomerProfile.
    public User User { get; set; } = null!;
}
