namespace MarketTJ.Application.Dto.ReviewDto;

public class GetReviewDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    // По прямому запросу пользователя (2026-07-31): отзывы — публичная витрина
    // хозяйства, покупатель сам согласился, что его отзыв виден всем (включая
    // фермера, которому отзыв адресован) — общий ярлык "Покупатель Market.tj"
    // выглядел неубедительно и скрывал то, что фермеру и так нужно видеть.
    public string? CustomerFullName { get; set; }
    public int FarmerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
