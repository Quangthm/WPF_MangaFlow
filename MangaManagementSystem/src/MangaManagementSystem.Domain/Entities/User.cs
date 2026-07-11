using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Role? Role { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
    }
}
