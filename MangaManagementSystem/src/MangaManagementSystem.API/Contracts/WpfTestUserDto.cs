namespace MangaManagementSystem.API.Contracts;

public sealed record WpfTestUserDto(
    string UserId,
    string Username,
    string DisplayName,
    string RoleCode
);
