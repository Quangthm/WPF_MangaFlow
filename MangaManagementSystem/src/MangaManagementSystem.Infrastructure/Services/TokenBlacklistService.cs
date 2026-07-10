using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MangaManagementSystem.Infrastructure.Services;

public sealed class TokenBlacklistService : ITokenBlacklistService
{
    private const string CacheKeyPrefix = "token-blacklist:";
    private readonly IMemoryCache _memoryCache;

    public TokenBlacklistService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void BlacklistToken(string token, DateTime expiryUtc)
    {
        var key = BuildCacheKey(token);
        var ttl = expiryUtc - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
        {
            _memoryCache.Set(key, true, ttl);
        }
    }

    public bool IsTokenBlacklisted(string token)
    {
        var key = BuildCacheKey(token);
        return _memoryCache.TryGetValue<bool>(key, out var blacklisted) && blacklisted;
    }

    private static string BuildCacheKey(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return CacheKeyPrefix + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
