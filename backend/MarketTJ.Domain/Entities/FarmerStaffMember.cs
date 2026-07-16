using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class FarmerStaffMember
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    public int UserId { get; set; }
    public StaffPermissions Permissions { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // FarmerProfile 1 — many FarmerStaffMember / User 1 — 0..1 FarmerStaffMember.
    public FarmerProfile FarmerProfile { get; set; } = null!;
    public User User { get; set; } = null!;
}
