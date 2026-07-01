using System.Security.Claims;
using Bilreg.Infrastructure.Shared.User;
using Microsoft.AspNetCore.Authorization;

namespace Bilreg.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.PermissionId))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var roleClaims = context.User.FindAll(ClaimTypes.Role)
            .Concat(context.User.FindAll("role"))
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (roleClaims.Count == 0)
            return Task.CompletedTask;

        var permissions = RolePermissionDto.ListData()
            .Where(p => roleClaims.Contains(p.RoleId, StringComparer.Ordinal))
            .Select(p => p.PermissionId)
            .ToHashSet(StringComparer.Ordinal);

        if (permissions.Contains(requirement.PermissionId))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
