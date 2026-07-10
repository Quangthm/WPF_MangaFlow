namespace MangaManagementSystem.Application.Interfaces;

public interface ITokenBlacklistService
{
    void BlacklistToken(string token, DateTime expiryUtc);
    bool IsTokenBlacklisted(string token);
}
