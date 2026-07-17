namespace MarketTJ.Application.Dto.CommissionDto;

public class GetCommissionDto
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public decimal Percentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
}
