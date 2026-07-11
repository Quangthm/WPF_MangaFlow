using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", "auth");
            builder.HasKey(u => u.UserId);
            builder.Property(u => u.UserId).ValueGeneratedOnAdd();
            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
        }
    }
}
