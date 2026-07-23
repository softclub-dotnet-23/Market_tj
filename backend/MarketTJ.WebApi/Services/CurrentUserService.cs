using System.Security.Claims;
using MarketTJ.Application.Interfaces.Services;

namespace MarketTJ.WebApi.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => Principal?.FindFirst(ClaimTypes.Role)?.Value;
    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value;
}
