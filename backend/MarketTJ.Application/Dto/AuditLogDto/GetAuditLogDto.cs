namespace MarketTJ.Application.Dto.AuditLogDto;

public class GetAuditLogDto
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public int EntityId { get; set; }
    public string Details { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
