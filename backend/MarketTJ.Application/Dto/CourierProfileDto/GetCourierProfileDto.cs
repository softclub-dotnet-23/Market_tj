namespace MarketTJ.Application.Dto.CourierProfileDto;

public class GetCourierProfileDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TransportType { get; set; } = null!;
    public string VehicleNumber { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
