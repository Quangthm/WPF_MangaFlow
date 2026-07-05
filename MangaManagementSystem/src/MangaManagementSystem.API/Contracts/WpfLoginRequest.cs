using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts;

public record WpfLoginRequest(
    [Required] string Username,
    [Required] string Password
);
