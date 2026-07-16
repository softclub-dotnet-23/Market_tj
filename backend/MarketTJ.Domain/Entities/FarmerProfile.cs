using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class FarmerProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FarmName { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public string Village { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Description { get; set; }
    public FarmerVerificationStatus VerificationStatus { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int? VerifiedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // User 1 — 1 FarmerProfile.
    public User User { get; set; } = null!;

    // Admin (User), который подтвердил фермера — необязательная связь.
    public User? VerifiedByAdmin { get; set; }

    // FarmerProfile 1 — many ProductListing.
    public ICollection<ProductListing> ProductListings { get; set; } = new List<ProductListing>();
}
