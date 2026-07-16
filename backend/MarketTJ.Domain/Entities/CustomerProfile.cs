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

    // CustomerProfile 1 — many CartItem / Order / Review.
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
