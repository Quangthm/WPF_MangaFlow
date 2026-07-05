using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MangaManagementSystem.API.Controllers;

[ApiController]
[Route("api/wpf/auth")]
public sealed class WpfAuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WpfAuthController> _logger;

    public WpfAuthController(
        IAuthService authService,
        IUserService userService,
        IConfiguration configuration,
        ILogger<WpfAuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] WpfLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _authService.LoginAsync(
            new LoginDto(request.Username, request.Password));

        if (!result.Succeeded || result.User is null || string.IsNullOrWhiteSpace(result.RoleName))
        {
            return Unauthorized(new ApiErrorResponse(
                result.ErrorMessage ?? "Invalid credentials"));
        }

        var expiresAtUtc = DateTime.UtcNow.AddDays(14);
        var accessToken = GenerateJwtToken(result.User, result.RoleName, expiresAtUtc);

        var response = new WpfLoginResponse(
            UserId: result.User.UserId.ToString(),
            Username: result.User.Username,
            DisplayName: result.User.DisplayName,
            RoleCode: result.RoleName.ToUpperInvariant(),
            Token: accessToken,
            Email: result.User.Email
        );

        return Ok(response);
    }

    [HttpGet("test-users")]
    public async Task<IActionResult> GetTestUsersAsync(CancellationToken cancellationToken)
    {
        var activeUsers = await _userService.GetUsersByStatusAsync("ACTIVE");

        var testUsers = activeUsers.Select(u => new WpfTestUserDto(
            UserId: u.UserId.ToString(),
            Username: u.Username,
            DisplayName: u.DisplayName,
            RoleCode: (u.RoleName ?? "").ToUpperInvariant()
        )).ToList();

        return Ok(testUsers);
    }

    private string GenerateJwtToken(
        UserDto user,
        string roleName,
        DateTime expiresAtUtc)
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
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName),
            new("user_id", user.UserId.ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
