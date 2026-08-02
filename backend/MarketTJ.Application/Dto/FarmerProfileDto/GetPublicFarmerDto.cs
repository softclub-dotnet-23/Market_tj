namespace MarketTJ.Application.Dto.FarmerProfileDto;

// Публичная витрина каталога (audit 2026-08-02) — только подтверждённые
// фермеры, с уже посчитанными сервером агрегатами. Раньше фронт тянул себе
// /farmer-profiles и /reviews целиком и фильтровал/агрегировал в браузере.
public class GetPublicFarmerDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FarmName { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public string Village { get; set; } = null!;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int ProductCount { get; set; }
    public List<string> Tags { get; set; } = new();
}
