using MangaManagementSystem.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> CreateUserAsync(CreateUserDto dto);

        Task<UserDto?> GetUserByIdAsync(Guid id);

        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName);

        Task ResetPasswordAsync(Guid userId, string newPassword);

        Task RecordProfileAuditAsync(Guid actorUserId, string actionCode, string detailJson);
    }
}
