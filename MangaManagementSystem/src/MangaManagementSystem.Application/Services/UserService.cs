using MangaManagementSystem.Application.DTOs.Auth;
using Microsoft.Extensions.Logging;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Mappers;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Services
{
    public class UserService : IUserService
    {
        private static readonly HashSet<string> AllowedRoleNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",
            "Mangaka",
            "Assistant",
            "Tantou Editor",
            "Editorial Board Member",
            "Editorial Board Chief"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var roleName = dto.RoleName.Trim();
            EnsureValidRoleName(roleName);

            var username = dto.Username.Trim();
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            var roles = await _unitOfWork.Roles.GetAllAsync();
            var role = roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role is null)
                throw new InvalidOperationException($"Role '{roleName}' not found.");

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                RoleId = role.RoleId,
                Username = username,
                PasswordHash = passwordHash
            };

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Users.GetByIdAsync(newUser.UserId);
            return created!.ToDto();
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Users.GetByIdAsync(id);
            return entity is null ? null : entity.ToDto();
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName)
        {
            var entities = await _unitOfWork.Users.GetByRoleNameAsync(roleName);
            return entities.Select(user => user.ToDto());
        }

        public async Task ResetPasswordAsync(Guid userId, string newPassword)
        {
            await RequireExistingUserAsync(userId);

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new InvalidOperationException("New password cannot be empty.");

            if (newPassword.Length < 8)
                throw new InvalidOperationException("New password must be at least 8 characters.");

            var passwordHash = _passwordHasher.HashPassword(newPassword);
            await _unitOfWork.Users.ResetPasswordViaProcAsync(userId, passwordHash);
        }

        public async Task RecordProfileAuditAsync(
            Guid actorUserId,
            string actionCode,
            string detailJson)
        {
            var user = await RequireExistingUserAsync(actorUserId);

            if (string.IsNullOrWhiteSpace(actionCode))
                throw new InvalidOperationException("Audit action code is required.");

            var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
            var actorRoleName = role?.RoleName ?? user.Role?.RoleName;

            var entity = new AuditEvent
            {
                OccurredAtUtc = DateTime.UtcNow,
                ActorUserId = actorUserId,
                ActorRoleName = actorRoleName,
                ActionCode = actionCode.Trim().ToUpperInvariant(),
                EntityType = "USER",
                EntityId = actorUserId.ToString(),
                DetailJson = string.IsNullOrWhiteSpace(detailJson) ? null : detailJson
            };

            await _unitOfWork.AuditEvents.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<User> RequireExistingUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user is null)
                throw new InvalidOperationException($"User {userId} was not found.");

            return user;
        }

        private static void EnsureValidRoleName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName) || !AllowedRoleNames.Contains(roleName))
            {
                throw new InvalidOperationException(
                    $"Role '{roleName}' is invalid. Allowed roles are: {string.Join(", ", AllowedRoleNames)}.");
            }
        }

        private static string NormalizeRoleName(string roleName)
            => roleName.Trim();
    }
}
