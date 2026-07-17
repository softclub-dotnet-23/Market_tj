namespace MarketTJ.Application.Dto.ReviewDto;

public class UpdateReviewDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
