using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.RefundRequestDto;

public class UpdateRefundRequestDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string Reason { get; set; } = null!;
    public decimal Amount { get; set; }
    public RefundStatus Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int? ProcessedByAdminId { get; set; }
}
