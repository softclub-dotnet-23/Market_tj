using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.FarmerProfileDto;

public class CreateFarmerProfileDto
{
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
}
