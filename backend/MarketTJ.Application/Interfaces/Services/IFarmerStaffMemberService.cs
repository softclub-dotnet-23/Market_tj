using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.FarmerStaffMemberDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IFarmerStaffMemberService
{
    Task<Result<IEnumerable<GetFarmerStaffMemberDto>>> GetAllAsync();
    Task<Result<GetFarmerStaffMemberDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateFarmerStaffMemberDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateFarmerStaffMemberDto dto);
    Task<Result<string>> DeleteAsync(int id);

    // Дополнено по явному запросу пользователя — фермер не может искать
    // пользователей по UserId (это Admin-only /api/users), поэтому находит
    // будущего сотрудника по email. Не различает "email не зарегистрирован"
    // и "уже сотрудник" в ответе наружу — тот же anti-enumeration принцип,
    // что и в AuthService.LoginAsync, распространённый и сюда по её просьбе.
    // При успехе отправляет сотруднику письмо на почту (не роняет операцию,
    // если письмо не ушло — см. реализацию).
    Task<Result<string>> CreateByEmailAsync(CreateFarmerStaffMemberByEmailDto dto);
}
