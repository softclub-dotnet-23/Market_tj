using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class FarmerDocument
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    public FarmerDocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = null!;
    public DocumentReviewStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }

    // FarmerProfile 1 — many FarmerDocument.
    public FarmerProfile FarmerProfile { get; set; } = null!;

    // User — Admin, проверивший документ (необязательная связь).
    public User? ReviewedByAdmin { get; set; }
}
