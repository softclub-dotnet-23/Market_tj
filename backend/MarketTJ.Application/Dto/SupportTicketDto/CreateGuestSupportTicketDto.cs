namespace MarketTJ.Application.Dto.SupportTicketDto;

// Обращение без регистрации — по прямому запросу пользователя ("чтобы без
// регистрации тоже могли писать"). Status/Priority здесь не передаются
// клиентом — сервис сам ставит Open/Normal, как для обычного нового обращения.
public class CreateGuestSupportTicketDto
{
    public string GuestName { get; set; } = null!;
    public string GuestEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Message { get; set; } = null!;
}
