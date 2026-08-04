using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.Admin;

public class ReviewCourierDocumentDto
{
    public DocumentReviewStatus Status { get; set; }
    public string? RejectionReason { get; set; }
}
