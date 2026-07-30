using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.Admin;

// Тот же набор полей, что и GetFarmerDocumentDto (фермер видит свои документы),
// плюс резолвленные FarmName/FarmerFullName — админ модерирует документы
// многих разных фермеров сразу и не должен вручную сверять FarmerProfileId.
public class GetAdminFarmerDocumentDto
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    public string FarmName { get; set; } = null!;
    public string? FarmerFullName { get; set; }
    public FarmerDocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = null!;
    public DocumentReviewStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }
}
