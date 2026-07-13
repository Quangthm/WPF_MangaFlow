using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        private IQueryable<User> UsersWithRole()
        {
            return _context.Users.Include(user => user.Role);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await UsersWithRole()
                .FirstOrDefaultAsync(user => user.Username == username);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(
            string usernameOrEmail)
        {
            return await UsersWithRole()
                .FirstOrDefaultAsync(user => user.Username == usernameOrEmail);
        }

        public async Task<IReadOnlyList<User>> GetByRoleNameAsync(
            string roleName)
        {
            return await UsersWithRole()
                .AsNoTracking()
                .Where(user =>
                    user.Role != null
                    && user.Role.RoleName == roleName)
                .OrderBy(user => user.Username)
                .ToListAsync();
        }

        public async Task ResetPasswordViaProcAsync(
            Guid userId,
            string passwordHash)
        {
            var conn = _context.Database.GetDbConnection();

            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                "auth.usp_User_ResetPassword";

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                new SqlParameter(
                    "@target_user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = userId
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@new_password_hash",
                    SqlDbType.NVarChar,
                    255)
                {
                    Value = passwordHash
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@actor_user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@reset_mode",
                    SqlDbType.NVarChar,
                    30)
                {
                    Value = "TOKEN_RESET"
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@reset_reason",
                    SqlDbType.NVarChar,
                    500)
                {
                    Value = "Password reset verified by OTP."
                });

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            await ReloadTrackedUserAsync(userId);
        }

        private async Task ReloadTrackedUserAsync(
            Guid userId)
        {
            var trackedUser =
                _context.Users.Local
                    .FirstOrDefault(
                        user => user.UserId == userId);

            if (trackedUser != null)
            {
                await _context.Entry(trackedUser)
                    .ReloadAsync();
            }
        }
    }
}
