using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SaleManagement.Data;

namespace SaleManagement.Services;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    public JwtBlacklistMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context )
    {
        var user = context.User;
        if (user.Identity.IsAuthenticated)
        {
            var jti = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti) && _cache.TryGetValue(jti, out _))
            {
                // Token is blacklisted
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("This token has been revoked.");
                return;
            }
        }

        await _next(context);
    }
}