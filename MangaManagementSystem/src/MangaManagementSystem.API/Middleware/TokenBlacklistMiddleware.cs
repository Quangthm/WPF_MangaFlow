using System.Net;
using System.Net.Mime;
using MangaManagementSystem.Application.Interfaces;

namespace MangaManagementSystem.API.Middleware;

public sealed class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (authHeader is not null
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && authHeader.Length > "Bearer ".Length)
        {
            var token = authHeader["Bearer ".Length..];

            if (blacklistService.IsTokenBlacklisted(token))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(
                    """{"message":"Token has been revoked. Please log in again."}""");
                return;
            }
        }

        await _next(context);
    }
}
