using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

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
    private readonly IConfiguration _configuration;
    private readonly ILogger<WpfAuthController> _logger;

    public WpfAuthController(
        IAuthService authService,
        IConfiguration configuration,
        ILogger<WpfAuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
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

            var expiresAtUtc = DateTime.UtcNow.AddDays(14);
            var accessToken = GenerateJwtToken(result.User, result.RoleName, expiresAtUtc);

            _logger.LogInformation("WPF login succeeded for user {Username} with role {Role} -> code {RoleCode}", request.Username, result.RoleName, roleCode);

            return Ok(new
            {
                userId = result.User.UserId.ToString(),
                username = result.User.Username,
                roleCode,
                token = accessToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WPF login failed with unexpected error for user {Username}", request.Username);
            return StatusCode(500, new { error = $"Internal server error: {ex.GetType().Name} — {ex.Message}" });
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
            new { userId = "00000000-0000-0000-0000-000000000001", username = "editor1", displayName = "Editor One", roleCode = "EDITOR" },
            new { userId = "00000000-0000-0000-0000-000000000002", username = "boardchief1", displayName = "Board Chief One", roleCode = "BOARD_CHIEF" },
            new { userId = "00000000-0000-0000-0000-000000000003", username = "mangaka1", displayName = "Mangaka One", roleCode = "MANGAKA" },
        };

        return Ok(testUsers);
    }

    private string GenerateJwtToken(UserDto user, string roleName, DateTime expiresAtUtc)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing.");

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is missing.");

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, roleName),
            new("user_id", user.UserId.ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Request model cho WPF mini client login.
/// </summary>
public sealed record WpfLoginRequest(string Username, string Password);
