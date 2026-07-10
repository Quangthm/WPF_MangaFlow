using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MangaManagementSystem.Application.Services;

public sealed class JwtService : IJwtService
{
    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly double _expireMinutes;

    public JwtService(IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing.");
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing.");

        _expireMinutes = double.TryParse(configuration["Jwt:ExpireMinutes"], out var minutes)
            ? minutes
            : 120;

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(UserDto user, string roleName)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_expireMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName),
            new("user_id", user.UserId.ToString())
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
