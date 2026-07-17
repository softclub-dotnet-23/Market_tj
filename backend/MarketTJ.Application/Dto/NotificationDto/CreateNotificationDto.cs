namespace MarketTJ.Application.Dto.NotificationDto;

public class CreateNotificationDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
}
