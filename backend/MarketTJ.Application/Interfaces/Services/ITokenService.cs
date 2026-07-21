using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
