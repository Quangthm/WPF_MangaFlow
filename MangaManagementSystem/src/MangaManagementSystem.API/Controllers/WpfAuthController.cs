using System.IdentityModel.Tokens.Jwt;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers;

[ApiController]
[Route("api/wpf/auth")]
public sealed class WpfAuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly IUserService _userService;
    private readonly ILogger<WpfAuthController> _logger;

    public WpfAuthController(
        IAuthService authService,
        IJwtService jwtService,
        ITokenBlacklistService blacklistService,
        IUserService userService,
        ILogger<WpfAuthController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _blacklistService = blacklistService;
        _userService = userService;
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

        var (accessToken, expiresAtUtc) = _jwtService.GenerateToken(result.User, result.RoleName);

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

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var token = ExtractBearerToken();
        if (token is null)
        {
            return BadRequest(new ApiErrorResponse("No token found in request."));
        }

        var expiry = ExtractTokenExpiry(token);
        if (expiry.HasValue)
        {
            _blacklistService.BlacklistToken(token, expiry.Value);
            _logger.LogInformation("WPF token blacklisted (expires {Expiry}).", expiry.Value);
        }

        return Ok(new ApiMessageResponse("Logged out successfully."));
    }

    private string? ExtractBearerToken()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null
            || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            || authHeader.Length <= "Bearer ".Length)
        {
            return null;
        }

        return authHeader["Bearer ".Length..];
    }

    private static DateTime? ExtractTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var expValue = jwtToken.Claims
                .FirstOrDefault(c => c.Type == "exp")?
                .Value;

            if (expValue is not null
                && long.TryParse(expValue, out var expUnix))
            {
                return DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            }
        }
        catch
        {
            // Ignore malformed tokens
        }

        return null;
    }
}
