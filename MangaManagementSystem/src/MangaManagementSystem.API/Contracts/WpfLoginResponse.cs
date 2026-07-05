namespace MangaManagementSystem.API.Contracts;

public sealed record WpfLoginResponse(
    string UserId,
    string Username,
    string DisplayName,
    string RoleCode,
    string Token,
    string Email
);
