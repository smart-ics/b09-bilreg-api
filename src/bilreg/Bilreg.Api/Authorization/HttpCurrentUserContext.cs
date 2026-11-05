using System.Security.Claims;
using Bilreg.Application.Shared;

namespace Bilreg.Api.Authorization;

public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public string GetActorUserId()
    {
        var userId = TryGetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("User is not authenticated.");

        return userId;
    }

    public IReadOnlyList<string> GetRoles()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return [];

        return user.FindAll(ClaimTypes.Role)
            .Concat(user.FindAll("role"))
            .Select(claim => claim.Value?.Trim() ?? string.Empty)
            .Where(role => role.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string GetClientIpAddress()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return string.Empty;

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }

    private string? TryGetUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.Identity?.Name;
    }
}
