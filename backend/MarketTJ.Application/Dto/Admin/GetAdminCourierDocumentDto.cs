using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.Admin;

public class GetAdminCourierDocumentDto
{
    public int Id { get; set; }
    public int CourierProfileId { get; set; }
    public string? CourierFullName { get; set; }
    public string? CourierPhoneNumber { get; set; }
    public CourierDocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = null!;
    public DocumentReviewStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }
}
