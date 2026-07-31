namespace MangaManagementSystem.WpfMini.Models;

public sealed class CurrentUserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }

    public string DisplayName => Username;

    public bool IsLoggedIn =>
        !string.IsNullOrWhiteSpace(UserId)
        && Guid.TryParse(UserId, out _)
        && !string.IsNullOrWhiteSpace(AccessToken);

    public bool IsMangaka => RoleCode == "MANGAKA";
    public bool IsEditor => RoleCode == "EDITOR";
    public bool IsBoardChief => RoleCode == "BOARD_CHIEF";
    public bool IsBoardMember => RoleCode == "BOARD_MEMBER";
    public bool IsBoardRole => IsBoardChief || IsBoardMember;
}
