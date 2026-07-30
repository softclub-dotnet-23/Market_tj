using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerStaffMemberDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class FarmerStaffMemberService(
    IFarmerStaffMemberRepository farmerStaffMemberRepository,
    IFarmerProfileRepository farmerProfileRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    IEmailSender emailSender,
    ILogger<FarmerStaffMemberService> logger) : IFarmerStaffMemberService
{
    // Audit 2026-07-28, находка 2.2 (IDOR): владелец фермы (FarmerProfile) видит/
    // управляет своими сотрудниками; сотрудник видит собственную запись.
    private async Task<bool> CanAccessAsync(FarmerStaffMember member)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;
        if (member.UserId == currentUser.UserId)
            return true;

        var myFarmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return myFarmerProfile is not null && myFarmerProfile.Id == member.FarmerProfileId;
    }

    public async Task<Result<IEnumerable<GetFarmerStaffMemberDto>>> GetAllAsync()
    {
        try
        {
            var members = await farmerStaffMemberRepository.GetAllAsync();

            if (!currentUser.IsAdmin())
            {
                var filtered = new List<FarmerStaffMember>();
                foreach (var member in members)
                {
                    if (await CanAccessAsync(member))
                        filtered.Add(member);
                }
                members = filtered;
            }

            return Result<IEnumerable<GetFarmerStaffMemberDto>>.Ok(members.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка сотрудников фермеров");
            return Result<IEnumerable<GetFarmerStaffMemberDto>>.Fail("Не удалось получить список сотрудников", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetFarmerStaffMemberDto?>> GetByIdAsync(int id)
    {
        try
        {
            var member = await farmerStaffMemberRepository.GetByIdAsync(id);
            if (member is null)
                return Result<GetFarmerStaffMemberDto?>.Fail("Сотрудник не найден", ErrorType.NotFound);

            if (!await CanAccessAsync(member))
                return Result<GetFarmerStaffMemberDto?>.Fail("Нет доступа к этой записи", ErrorType.Forbidden);

            return Result<GetFarmerStaffMemberDto?>.Ok(ToGetDto(member));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении сотрудника {Id}", id);
            return Result<GetFarmerStaffMemberDto?>.Fail("Не удалось получить сотрудника", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateFarmerStaffMemberDto dto)
    {
        try
        {
            var validation = FarmerStaffMemberValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerProfileId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            // IDOR-guard (audit 2026-07-28, находка 2.2): добавлять сотрудников
            // может только сам владелец фермы — не проверка на member.UserId,
            // т.к. на момент создания записи ещё нет.
            if (!currentUser.IsAdmin())
            {
                var myFarmerProfile = currentUser.UserId is null ? null : await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (myFarmerProfile is null || myFarmerProfile.Id != dto.FarmerProfileId)
                    return Result<string>.Fail("Нельзя добавить сотрудника в чужую ферму", ErrorType.Forbidden);
            }

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            // Раздел 8.26 ТЗ: один логин = один сотрудник.
            var all = await farmerStaffMemberRepository.GetAllAsync();
            if (all.Any(m => m.UserId == dto.UserId))
                return Result<string>.Fail("Этот пользователь уже привязан как сотрудник", ErrorType.Conflict);

            var member = new FarmerStaffMember
            {
                FarmerProfileId = dto.FarmerProfileId,
                UserId = dto.UserId,
                Permissions = dto.Permissions,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await farmerStaffMemberRepository.AddAsync(member);
            return Result<string>.Ok("Сотрудник добавлен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании сотрудника");
            return Result<string>.Fail("Не удалось добавить сотрудника", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateFarmerStaffMemberDto dto)
    {
        try
        {
            var validation = FarmerStaffMemberValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var member = await farmerStaffMemberRepository.GetByIdAsync(id);
            if (member is null)
                return Result<string>.Fail("Сотрудник не найден", ErrorType.NotFound);

            // IDOR-guard: изменять права/статус сотрудника может только владелец
            // фермы (не сам сотрудник — иначе он мог бы выдать себе больше прав).
            if (!currentUser.IsAdmin())
            {
                var myFarmerProfile = currentUser.UserId is null ? null : await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (myFarmerProfile is null || myFarmerProfile.Id != member.FarmerProfileId)
                    return Result<string>.Fail("Нет доступа к этой записи", ErrorType.Forbidden);
            }

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerProfileId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            var all = await farmerStaffMemberRepository.GetAllAsync();
            if (all.Any(m => m.Id != id && m.UserId == dto.UserId))
                return Result<string>.Fail("Этот пользователь уже привязан как сотрудник", ErrorType.Conflict);

            member.FarmerProfileId = dto.FarmerProfileId;
            member.UserId = dto.UserId;
            member.Permissions = dto.Permissions;
            member.IsActive = dto.IsActive;
            member.UpdatedAt = DateTime.UtcNow;

            await farmerStaffMemberRepository.UpdateAsync(member);
            return Result<string>.Ok("Данные сотрудника обновлены");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении сотрудника {Id}", id);
            return Result<string>.Fail("Не удалось обновить данные сотрудника", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateByEmailAsync(CreateFarmerStaffMemberByEmailDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<string>.Fail("Email обязателен", ErrorType.Validation);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerProfileId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            // IDOR-guard (audit 2026-07-28, находка 2.2), тот же принцип, что и
            // в CreateAsync — добавлять сотрудников может только сам владелец фермы.
            if (!currentUser.IsAdmin())
            {
                var myFarmerProfile = currentUser.UserId is null ? null : await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (myFarmerProfile is null || myFarmerProfile.Id != dto.FarmerProfileId)
                    return Result<string>.Fail("Нельзя добавить сотрудника в чужую ферму", ErrorType.Forbidden);
            }

            var user = await userRepository.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant());

            var all = await farmerStaffMemberRepository.GetAllAsync();
            if (user is null || all.Any(m => m.UserId == user.Id))
                return Result<string>.Fail("Не удалось добавить сотрудника — проверьте email", ErrorType.NotFound);

            var member = new FarmerStaffMember
            {
                FarmerProfileId = dto.FarmerProfileId,
                UserId = user.Id,
                Permissions = dto.Permissions,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await farmerStaffMemberRepository.AddAsync(member);

            try
            {
                await emailSender.SendAsync(
                    user.Email,
                    "Вас добавили сотрудником на Market.tj",
                    $"<p>Здравствуйте, {user.FullName}!</p><p>Вас добавили сотрудником хозяйства «{farmerProfile.FarmName}» на платформе Market.tj. Войдите в свой аккаунт, чтобы начать работу.</p>");
            }
            catch (Exception emailEx)
            {
                // Письмо — не критичная часть операции: сотрудник уже добавлен
                // в базе, повторная попытка при сбое SMTP лишь наткнётся на
                // "уже сотрудник" и введёт в заблуждение — поэтому не роняем
                // результат, только логируем.
                logger.LogWarning(emailEx, "Не удалось отправить письмо новому сотруднику {UserId}", user.Id);
            }

            return Result<string>.Ok("Сотрудник добавлен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении сотрудника по email");
            return Result<string>.Fail("Не удалось добавить сотрудника", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var member = await farmerStaffMemberRepository.GetByIdAsync(id);
            if (member is null)
                return Result<string>.Fail("Сотрудник не найден", ErrorType.NotFound);

            if (!currentUser.IsAdmin())
            {
                var myFarmerProfile = currentUser.UserId is null ? null : await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (myFarmerProfile is null || myFarmerProfile.Id != member.FarmerProfileId)
                    return Result<string>.Fail("Нет доступа к этой записи", ErrorType.Forbidden);
            }

            await farmerStaffMemberRepository.DeleteAsync(member);
            return Result<string>.Ok("Сотрудник удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении сотрудника {Id}", id);
            return Result<string>.Fail("Не удалось удалить сотрудника", ErrorType.InternalServerError);
        }
    }

    private static GetFarmerStaffMemberDto ToGetDto(FarmerStaffMember member) => new()
    {
        Id = member.Id,
        FarmerProfileId = member.FarmerProfileId,
        UserId = member.UserId,
        Permissions = member.Permissions,
        IsActive = member.IsActive,
        CreatedAt = member.CreatedAt,
        UpdatedAt = member.UpdatedAt
    };
}
