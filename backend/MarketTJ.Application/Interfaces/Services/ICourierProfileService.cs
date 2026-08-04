using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.CourierProfileDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ICourierProfileService
{
    Task<Result<IEnumerable<GetCourierProfileDto>>> GetAllAsync();
    Task<Result<GetCourierProfileDto?>> GetByIdAsync(int id);
    // Возвращает Id созданного профиля (не просто сообщение) — по прямому
    // запросу пользователя (2026-08-04): фронтенд-форма регистрации курьера
    // сразу после создания профиля загружает документы верификации, для
    // чего нужен CourierProfileId, а не сообщение об успехе.
    Task<Result<int>> CreateAsync(CreateCourierProfileDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateCourierProfileDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
