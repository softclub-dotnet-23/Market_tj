using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.CourierDocumentDto;

public class GetCourierDocumentDto
{
    public int Id { get; set; }
    public int CourierProfileId { get; set; }
    public CourierDocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = null!;
    public DocumentReviewStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }
}
