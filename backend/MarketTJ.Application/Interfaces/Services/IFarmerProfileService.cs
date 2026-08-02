using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.FarmerProfileDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IFarmerProfileService
{
    Task<Result<IEnumerable<GetFarmerProfileDto>>> GetAllAsync();
    Task<Result<GetFarmerProfileDto?>> GetByIdAsync(int id);

    // Публичная витрина каталога (audit 2026-08-02) — только подтверждённые
    // фермеры, с рейтингом/числом отзывов/числом активных объявлений/тегами
    // категорий, посчитанными сервером. GetAllAsync выше остаётся как есть —
    // им пользуется админка, которой нужны фермеры любого статуса.
    Task<Result<IEnumerable<GetPublicFarmerDto>>> GetPublicCatalogAsync();
    Task<Result<string>> CreateAsync(CreateFarmerProfileDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateFarmerProfileDto dto);
    Task<Result<string>> DeleteAsync(int id);

    // Отдельный лёгкий toggle, а не через UpdateAsync — тому нужны ВСЕ поля
    // профиля разом (см. комментарий у UpdateFarmerProfileDto), это лишнее
    // для одной галочки.
    Task<Result<string>> SetAutoReplyAsync(int id, bool enabled);
}
