using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.PaymentDto;

public class UpdatePaymentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? TransactionReference { get; set; }
}
