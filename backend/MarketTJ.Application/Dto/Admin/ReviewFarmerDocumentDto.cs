using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.Admin;

public class ReviewFarmerDocumentDto
{
    // Approved — документ принят, Rejected — отклонён (тогда RejectionReason обязателен).
    public DocumentReviewStatus Status { get; set; }
    public string? RejectionReason { get; set; }
}
