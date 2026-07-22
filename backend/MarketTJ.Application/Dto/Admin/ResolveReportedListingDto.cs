using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.Admin;

public class ResolveReportedListingDto
{
    // Reviewed — жалоба подтверждена, Dismissed — отклонена как необоснованная.
    public ReportStatus Status { get; set; }
}
