using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

// Зеркалит FarmerDocument (см. FarmerDocument.cs) — тот же паттерн
// Pending/Approved/Rejected (DocumentReviewStatus переиспользуется, он уже
// общий), только владелец — CourierProfile, а не FarmerProfile.
public class CourierDocument
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

    public CourierProfile CourierProfile { get; set; } = null!;
    public User? ReviewedByAdmin { get; set; }
}
