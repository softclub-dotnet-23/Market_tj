namespace MarketTJ.Application.Dto.CourierProfileDto;

public class CreateCourierProfileDto
{
    public int UserId { get; set; }
    public string TransportType { get; set; } = null!;
    public string VehicleNumber { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
}
