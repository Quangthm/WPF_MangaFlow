using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Auth
{
    public record UserDto(
        Guid UserId,
        string Username,
        string? RoleName
    );

    public record CreateUserDto(
        [Required][MaxLength(30)] string RoleName,
        [Required][MaxLength(50)] string Username,
        [Required][MaxLength(255)] string Password
    );

    public record UpdateUserDto(
        [Required] Guid UserId,
        [Required][MaxLength(30)] string RoleName,
        [Required][MaxLength(50)] string Username
    );
}
