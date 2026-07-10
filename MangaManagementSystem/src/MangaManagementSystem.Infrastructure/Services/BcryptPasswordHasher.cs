using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Infrastructure.Services
{
    public class BcryptPasswordHasher : IPasswordHasher
    {
        private readonly ILogger<BcryptPasswordHasher> _logger;

        public BcryptPasswordHasher(ILogger<BcryptPasswordHasher> logger)
        {
            _logger = logger;
        }

        public string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                _logger.LogWarning("VerifyPassword called with null or empty hash.");
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BCrypt verification failed for hash starting with '{Prefix}'.", passwordHash[..Math.Min(passwordHash.Length, 20)]);
                return false;
            }
        }
    }
}
