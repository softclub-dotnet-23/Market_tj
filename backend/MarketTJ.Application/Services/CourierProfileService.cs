using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CourierProfileDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class CourierProfileService(
    ICourierProfileRepository courierProfileRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    ICourierDocumentService courierDocumentService,
    IGoogleGeocodingService geocodingService,
    ILogger<CourierProfileService> logger) : ICourierProfileService
{
    public async Task<Result<IEnumerable<GetCourierProfileDto>>> GetAllAsync()
    {
        try
        {
            var profiles = await courierProfileRepository.GetAllAsync();

            // Audit 2026-07-28, находка 2.2 (IDOR): не публичная витрина —
            // Admin (диспетчеризация доставок) видит всех, остальные — только себя.
            if (!currentUser.IsAdmin())
                profiles = profiles.Where(p => p.UserId == currentUser.UserId).ToList();

            return Result<IEnumerable<GetCourierProfileDto>>.Ok(profiles.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка профилей курьеров");
            return Result<IEnumerable<GetCourierProfileDto>>.Fail("Не удалось получить список профилей курьеров", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetCourierProfileDto?>> GetByIdAsync(int id)
    {
        try
        {
            var profile = await courierProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<GetCourierProfileDto?>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            if (!currentUser.CanAccess(profile.UserId))
                return Result<GetCourierProfileDto?>.Fail("Нет доступа к этому профилю", ErrorType.Forbidden);

            return Result<GetCourierProfileDto?>.Ok(ToGetDto(profile));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении профиля курьера {Id}", id);
            return Result<GetCourierProfileDto?>.Fail("Не удалось получить профиль курьера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<int>> CreateAsync(CreateCourierProfileDto dto)
    {
        try
        {
            var validation = CourierProfileValidator.ValidateCreate(dto);
            if (validation is not null)
                return Result<int>.Fail(validation.Error!, validation.ErrorType!.Value);

            if (!currentUser.CanAccess(dto.UserId))
                return Result<int>.Fail("Нельзя создать профиль для другого пользователя", ErrorType.Forbidden);

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<int>.Fail("Пользователь не найден", ErrorType.NotFound);

            // Раздел 9 ТЗ: User 1 — 1 CourierProfile.
            var all = await courierProfileRepository.GetAllAsync();
            if (all.Any(c => c.UserId == dto.UserId))
                return Result<int>.Fail("У этого пользователя уже есть профиль курьера", ErrorType.Conflict);

            var profile = new CourierProfile
            {
                UserId = dto.UserId,
                TransportType = dto.TransportType,
                VehicleNumber = dto.VehicleNumber,
                Region = dto.Region,
                District = dto.District,
                Address = dto.Address,
                IsAvailable = dto.IsAvailable,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await GeocodeIfNeededAsync(profile);
            await courierProfileRepository.AddAsync(profile);
            return Result<int>.Ok(profile.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании профиля курьера");
            return Result<int>.Fail("Не удалось создать профиль курьера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateCourierProfileDto dto)
    {
        try
        {
            var validation = CourierProfileValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var profile = await courierProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            if (!currentUser.CanAccess(profile.UserId))
                return Result<string>.Fail("Нет доступа к этому профилю", ErrorType.Forbidden);

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            var all = await courierProfileRepository.GetAllAsync();
            if (all.Any(c => c.Id != id && c.UserId == dto.UserId))
                return Result<string>.Fail("У этого пользователя уже есть профиль курьера", ErrorType.Conflict);

            // По прямому запросу пользователя (2026-08-04): курьер не может
            // включить себе доступность для заказов, пока admin не одобрил
            // оба обязательных документа (права + техпаспорт). Admin сам
            // освобождён от гейта — та же схема, что и в ProductListingService
            // для фермеров без документов.
            if (dto.IsAvailable && !profile.IsAvailable && !currentUser.IsAdmin()
                && !await courierDocumentService.HasApprovedRequiredDocumentsAsync(id))
                return Result<string>.Fail(
                    "Нельзя стать доступным для заказов, пока документы не одобрены администратором",
                    ErrorType.Validation);

            var locationChanged = profile.Region != dto.Region || profile.District != dto.District || profile.Address != dto.Address;

            profile.UserId = dto.UserId;
            profile.TransportType = dto.TransportType;
            profile.VehicleNumber = dto.VehicleNumber;
            profile.Region = dto.Region;
            profile.District = dto.District;
            profile.Address = dto.Address;
            profile.IsAvailable = dto.IsAvailable;
            profile.IsActive = dto.IsActive;
            profile.UpdatedAt = DateTime.UtcNow;

            if (locationChanged)
                await GeocodeIfNeededAsync(profile);

            await courierProfileRepository.UpdateAsync(profile);
            return Result<string>.Ok("Профиль курьера обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении профиля курьера {Id}", id);
            return Result<string>.Fail("Не удалось обновить профиль курьера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var profile = await courierProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            if (!currentUser.CanAccess(profile.UserId))
                return Result<string>.Fail("Нет доступа к этому профилю", ErrorType.Forbidden);

            await courierProfileRepository.DeleteAsync(profile);
            return Result<string>.Ok("Профиль курьера удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении профиля курьера {Id}", id);
            return Result<string>.Fail("Не удалось удалить профиль курьера", ErrorType.InternalServerError);
        }
    }

    // Раздел "выбор курьера по карте в радиусе 40 км" (2026-08-05) — не
    // блокирует сохранение профиля при недоступности/ошибке Google Geocoding
    // API, тот же fail-open принцип, что и у перевода объявлений (Groq):
    // курьер без координат просто не участвует в подборе по расстоянию,
    // пока не пересохранит профиль после восстановления геокодирования.
    private async Task GeocodeIfNeededAsync(CourierProfile profile)
    {
        var addressParts = new[] { profile.Address, profile.District, profile.Region }.Where(p => !string.IsNullOrWhiteSpace(p));
        var fullAddress = string.Join(", ", addressParts);
        if (string.IsNullOrWhiteSpace(fullAddress))
            return;

        var geocoded = await geocodingService.GeocodeAsync(fullAddress);
        if (geocoded.IsSuccess)
        {
            profile.Latitude = geocoded.Data.Latitude;
            profile.Longitude = geocoded.Data.Longitude;
        }
        else
        {
            logger.LogWarning("Не удалось геокодировать адрес курьера (профиль {ProfileId}): {Error}", profile.Id, geocoded.Error);
            profile.Latitude = null;
            profile.Longitude = null;
        }
    }

    private static GetCourierProfileDto ToGetDto(CourierProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        TransportType = profile.TransportType,
        VehicleNumber = profile.VehicleNumber,
        Region = profile.Region,
        District = profile.District,
        Address = profile.Address,
        Latitude = profile.Latitude,
        Longitude = profile.Longitude,
        IsAvailable = profile.IsAvailable,
        IsActive = profile.IsActive,
        Rating = profile.Rating,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
