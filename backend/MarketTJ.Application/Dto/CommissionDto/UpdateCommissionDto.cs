namespace MarketTJ.Application.Dto.CommissionDto;

public class UpdateCommissionDto
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public decimal Percentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
