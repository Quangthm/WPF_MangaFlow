using System.Text.Json.Serialization;

namespace MangaManagementSystem.WpfMini.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("usernameOrEmail")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    [JsonPropertyName("user")]
    public LoginUserDto User { get; set; } = new();

    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class LoginUserDto
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("roleName")]
    public string? RoleName { get; set; }
}

public sealed class TestUserDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("roleCode")]
    public string RoleCode { get; set; } = string.Empty;
}