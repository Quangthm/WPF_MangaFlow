using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);

        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);

        Task<IReadOnlyList<User>> GetByRoleNameAsync(string roleName);

        Task ResetPasswordViaProcAsync(Guid userId, string passwordHash);
    }
}