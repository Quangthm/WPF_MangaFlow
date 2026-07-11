namespace MangaManagementSystem.WpfMini.Models;

public class CurrentUserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
    public bool IsMangaka => RoleCode == "MANGAKA";
    public bool IsEditor => RoleCode == "EDITOR";
    public bool IsBoardChief => RoleCode == "BOARD_CHIEF";
    public bool IsBoardMember => RoleCode == "BOARD_MEMBER";
    public bool IsBoardRole => IsBoardChief || IsBoardMember;
}
