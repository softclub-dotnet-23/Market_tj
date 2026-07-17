using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.FarmerDocumentDto;

public class CreateFarmerDocumentDto
{
    public int FarmerProfileId { get; set; }
    public FarmerDocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = null!;
    public DocumentReviewStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }
}
