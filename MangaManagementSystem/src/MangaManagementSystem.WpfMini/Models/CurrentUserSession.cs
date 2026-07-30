namespace MangaManagementSystem.WpfMini.Models;

public class CurrentUserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime? TokenExpiresAtUtc { get; set; }

    public string DisplayName => Username;

    public bool IsLoggedIn =>
        !string.IsNullOrEmpty(UserId)
        && Guid.TryParse(UserId, out _)
        && !string.IsNullOrWhiteSpace(AccessToken);

    public bool IsMangaka => RoleCode == "MANGAKA";
    public bool IsEditor => RoleCode == "EDITOR";
    public bool IsBoardChief => RoleCode == "BOARD_CHIEF";
    public bool IsBoardMember => RoleCode == "BOARD_MEMBER";
    public bool IsBoardRole => IsBoardChief || IsBoardMember;
}
