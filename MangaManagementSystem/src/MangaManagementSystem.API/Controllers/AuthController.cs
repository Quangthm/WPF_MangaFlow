using System.IdentityModel.Tokens.Jwt;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IJwtService jwtService,
        ITokenBlacklistService blacklistService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _blacklistService = blacklistService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _authService.LoginAsync(
            new LoginDto(request.UsernameOrEmail, request.Password));

        if (!result.Succeeded || result.User is null || string.IsNullOrWhiteSpace(result.RoleName))
        {
            return Unauthorized(new ApiErrorResponse(
                result.ErrorMessage ?? "Invalid credentials"));
        }

        var (accessToken, expiresAtUtc) = _jwtService.GenerateToken(result.User, result.RoleName);

        var response = new LoginResponse(
            result.User,
            result.RoleName,
            accessToken,
            expiresAtUtc);

        return Ok(response);
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
            _logger.LogInformation("Token blacklisted (expires {Expiry}).", expiry.Value);
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
