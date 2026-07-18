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
    ILogger<FarmerStaffMemberService> logger) : IFarmerStaffMemberService
{
    public async Task<Result<IEnumerable<GetFarmerStaffMemberDto>>> GetAllAsync()
    {
        try
        {
            var members = await farmerStaffMemberRepository.GetAllAsync();
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

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var member = await farmerStaffMemberRepository.GetByIdAsync(id);
            if (member is null)
                return Result<string>.Fail("Сотрудник не найден", ErrorType.NotFound);

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
