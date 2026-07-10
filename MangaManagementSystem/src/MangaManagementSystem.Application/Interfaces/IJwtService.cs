using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Application.Interfaces;

public interface IJwtService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(UserDto user, string roleName);
}
