namespace MarketTJ.Domain.Entities;

public class Commission
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public decimal Percentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }

    // Category 1 — many Commission (null = общая ставка по умолчанию).
    public Category? Category { get; set; }
}
