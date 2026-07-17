using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.ReportedListingDto;

public class UpdateReportedListingDto
{
    public int Id { get; set; }
    public int ProductListingId { get; set; }
    public int ReportedByUserId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Comment { get; set; }
    public ReportStatus Status { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }
}
