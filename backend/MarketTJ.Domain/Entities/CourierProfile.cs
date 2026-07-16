namespace MarketTJ.Domain.Entities;

public class CourierProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TransportType { get; set; } = null!;
    public string VehicleNumber { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // User 1 — 1 CourierProfile.
    public User User { get; set; } = null!;

    // CourierProfile 1 — many Delivery (CourierId у Delivery nullable).
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
