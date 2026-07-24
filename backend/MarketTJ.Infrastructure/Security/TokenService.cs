using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MarketTJ.Infrastructure.Security;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public int AccessTokenExpiryMinutes => int.Parse(configuration.GetSection("Jwt")["ExpiryMinutes"]!);

    // Раздел 16 ТЗ ("дополнительно"): refresh token долгоживущий — недели/месяцы,
    // а не минуты, поэтому отдельная настройка, а не производная от access-токена.
    public int RefreshTokenExpiryDays => int.Parse(configuration.GetSection("Jwt")["RefreshTokenExpiryDays"] ?? "30");

    public string GenerateToken(User user)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"]!;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Криптографически случайная строка (не JWT) — предъявляется на
    // /api/auth/refresh для обмена на новую пару токенов.
    public string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
