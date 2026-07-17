namespace MarketTJ.Application.Dto.CommissionDto;

public class CreateCommissionDto
{
    public int? CategoryId { get; set; }
    public decimal Percentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
