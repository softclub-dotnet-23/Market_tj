using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // User 1 — 1 FarmerProfile / CustomerProfile / CourierProfile.
    public FarmerProfile? FarmerProfile { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }
    public CourierProfile? CourierProfile { get; set; }

    // User 1 — many Notification.
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    // User 1 — many CartItem (как Customer).
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    // User 1 — many Order (как Customer / как Farmer) — два разных FK на User.
    public ICollection<Order> OrdersAsCustomer { get; set; } = new List<Order>();
    public ICollection<Order> OrdersAsFarmer { get; set; } = new List<Order>();

    // User 1 — many Review (как Customer / как Farmer).
    public ICollection<Review> ReviewsAsCustomer { get; set; } = new List<Review>();
    public ICollection<Review> ReviewsAsFarmer { get; set; } = new List<Review>();
}
