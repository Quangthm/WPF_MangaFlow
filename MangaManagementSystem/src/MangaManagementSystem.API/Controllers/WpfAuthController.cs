using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers;

[ApiController]
[Route("api/wpf/auth")]
public sealed class WpfAuthController : ControllerBase
{
    private static readonly Dictionary<string, string> RoleNameToCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tantou Editor"] = "EDITOR",
        ["Editorial Board Chief"] = "BOARD_CHIEF",
        ["Editorial Board Member"] = "BOARD_MEMBER",
        ["Mangaka"] = "MANGAKA",
        ["Assistant"] = "ASSISTANT",
        ["Admin"] = "ADMIN",
    };

    private readonly IAuthService _authService;
    private readonly ILogger<WpfAuthController> _logger;

    public WpfAuthController(
        IAuthService authService,
        ILogger<WpfAuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// WPF mini client login.
    /// POST /api/wpf/auth/login
    /// Body: { "username": "...", "password": "..." }
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] WpfLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Username and password are required." });
            }

            _logger.LogInformation("WPF login attempt for user: {Username}", request.Username);

            var result = await _authService.LoginAsync(
                new LoginDto(request.Username, request.Password));

            if (!result.Succeeded || result.User is null || string.IsNullOrWhiteSpace(result.RoleName))
            {
                _logger.LogWarning("WPF login failed for user {Username}: {Error}", request.Username, result.ErrorMessage);
                return Unauthorized(new { error = result.ErrorMessage ?? "Invalid credentials" });
            }

            var roleCode = MapRoleNameToCode(result.RoleName);

            _logger.LogInformation("WPF login succeeded for user {Username} with role {Role} -> code {RoleCode}", request.Username, result.RoleName, roleCode);

            return Ok(new
            {
                userId = result.User.UserId.ToString(),
                username = result.User.Username,
                roleCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WPF login failed with unexpected error for user {Username}", request.Username);
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
        }
    }

    private static string MapRoleNameToCode(string roleName)
    {
        if (RoleNameToCode.TryGetValue(roleName, out var code))
            return code;

        // fallback: uppercase with underscores
        return roleName
            .Replace(" ", "_")
            .ToUpperInvariant();
    }

    /// <summary>
    /// Trả về danh sách test users cho WPF mini client quick-login.
    /// GET /api/wpf/auth/test-users
    /// </summary>
    [HttpGet("test-users")]
    public IActionResult GetTestUsers()
    {
        var testUsers = new[]
        {
            new { username = "TestEditor1", displayName = "TestEditor1 (Tantou Editor)", roleCode = "EDITOR" },
            new { username = "TestBoardChief1", displayName = "TestBoardChief1 (Board Chief)", roleCode = "BOARD_CHIEF" },
            new { username = "TestBoardMember1", displayName = "TestBoardMember1 (Board Member)", roleCode = "BOARD_MEMBER" },
            new { username = "TestMangaka1", displayName = "TestMangaka1 (Mangaka)", roleCode = "MANGAKA" },
            new { username = "TestAssistant1", displayName = "TestAssistant1 (Assistant)", roleCode = "ASSISTANT" },
            new { username = "TestAdmin", displayName = "TestAdmin (Admin)", roleCode = "ADMIN" },
        };

        return Ok(testUsers);
    }

}

/// <summary>
/// Request model cho WPF mini client login.
/// </summary>
public sealed record WpfLoginRequest(string Username, string Password);
