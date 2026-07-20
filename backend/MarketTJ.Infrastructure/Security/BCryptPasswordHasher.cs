using MarketTJ.Application.Interfaces.Services;

namespace MarketTJ.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
